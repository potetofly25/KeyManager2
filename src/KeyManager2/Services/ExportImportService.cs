using potetofly25.KeyManager2.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace potetofly25.KeyManager2.Services
{
    /// <summary>
    /// 資格情報（<see cref="Credential"/>）のエクスポート／インポート処理を提供するサービスクラスです。
    /// アプリ内のデータをファイルへ安全に書き出し、またファイルから復元するための
    /// 暗号化およびシリアライズ処理をカプセル化します。
    /// </summary>
    public static class ExportImportService
    {
        // JsonSerializerOptions のインスタンスをキャッシュして再利用
        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new() { WriteIndented = true };

        /// <summary>
        /// すべての <see cref="Credential"/> を指定パスにエクスポートします。
        /// マスターパスワードが設定されている場合は <see cref="AdvancedEncryptionService"/> を用いて fileKey をラップし、
        /// 未設定の場合はユーザー指定のエクスポート用パスワードで fileKey を暗号化します。
        /// </summary>
        /// <param name="path">エクスポート先ファイルのフルパス。</param>
        /// <param name="exportPassword">
        /// マスターパスワードが未設定の場合に fileKey を暗号化するために使用されるパスワード。
        /// マスターパスワードが設定されている場合は無視されます。
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// マスターパスワードが未設定かつ <paramref name="exportPassword"/> が null または空文字の場合にスローされます。
        /// </exception>
        public static void ExportAll(string path, string? exportPassword = null)
        {
            // バックアップファイル内の個々のレコード暗号化に使用する一時的な fileKey を生成（32 バイト）
            var fileKey = new byte[32];
            RandomNumberGenerator.Fill(fileKey);

            byte[] wrapped;

            // マスターパスワードが設定されている場合は AdvancedEncryptionService で fileKey をラップ
            if (AdvancedEncryptionService.IsMasterSet)
            {
                // fileKey を Base64 にして文字列として暗号化
                var b64 = Convert.ToBase64String(fileKey);
                var wrappedStr = AdvancedEncryptionService.EncryptString(b64);

                // ラップされた文字列を UTF-8 バイト列として保存対象とする
                wrapped = Encoding.UTF8.GetBytes(wrappedStr);
            }
            else
            {
                // マスターパスワードがない場合、ユーザー指定の exportPassword で fileKey を暗号化する
                if (string.IsNullOrEmpty(exportPassword))
                {
                    throw new InvalidOperationException("Export password required");
                }

                // ユーザーパスワード用のソルトを生成（16 バイト）
                var salt = RandomNumberGenerator.GetBytes(16);

                // PBKDF2 を使ってパスワードから 32 バイトの鍵を導出（反復回数 200,000）
                using var derive = new Rfc2898DeriveBytes(exportPassword, salt, 200_000, HashAlgorithmName.SHA256);
                var key = derive.GetBytes(32);

                // AES-GCM 用 IV（12 バイト）を生成
                var iv = RandomNumberGenerator.GetBytes(12);

                // 認証タグ用バッファ（16 バイト）
                var tag = new byte[16];

                byte[] cipher;

                // AES-GCM で fileKey を暗号化
                using (var aes = new AesGcm(key, 16))
                {
                    cipher = new byte[fileKey.Length];
                    aes.Encrypt(iv, fileKey, cipher, tag);
                }

                // salt + iv + cipher + tag を連結したラップ済みバッファを構築
                var wrappedBuf = new byte[salt.Length + iv.Length + cipher.Length + tag.Length];
                Buffer.BlockCopy(salt, 0, wrappedBuf, 0, salt.Length);
                Buffer.BlockCopy(iv, 0, wrappedBuf, salt.Length, iv.Length);
                Buffer.BlockCopy(cipher, 0, wrappedBuf, salt.Length + iv.Length, cipher.Length);
                Buffer.BlockCopy(tag, 0, wrappedBuf, salt.Length + iv.Length + cipher.Length, tag.Length);

                wrapped = wrappedBuf;
            }

            // エクスポートパッケージを構築し、fileKey のラップデータを Base64 で格納
            var pkg = new ExportPackage
            {
                WrappedFileKeyBase64 = Convert.ToBase64String(wrapped)
            };

            // 現在の資格情報を取得（マスターパスワードがある場合は生パスワードまで復号した状態で取得）
            var svc = new CredentialService();
            var records = svc.GetAll(tryDecrypt: AdvancedEncryptionService.IsMasterSet);

            // 各レコードのパスワードを fileKey で暗号化し、Record としてパッケージに詰める
            foreach (var r in records)
            {
                // パスワードを UTF-8 バイト列に変換
                var plain = Encoding.UTF8.GetBytes(r.Password ?? string.Empty);

                // AES-GCM 用 IV とタグを準備
                var iv = RandomNumberGenerator.GetBytes(12);
                var tag = new byte[16];
                byte[] cipher;

                // fileKey を使用してパスワードを暗号化
                using (var aes = new AesGcm(fileKey, 16))
                {
                    cipher = new byte[plain.Length];
                    aes.Encrypt(iv, plain, cipher, tag);
                }

                // iv + cipher + tag を連結したペイロードを構築
                var payload = new byte[iv.Length + cipher.Length + tag.Length];
                Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
                Buffer.BlockCopy(cipher, 0, payload, iv.Length, cipher.Length);
                Buffer.BlockCopy(tag, 0, payload, iv.Length + cipher.Length, tag.Length);

                // エクスポート用 Record としてパッケージに追加
                pkg.Records.Add(new Record
                {
                    Id = r.Id,
                    LoginId = r.LoginId,
                    Password = Convert.ToBase64String(payload),
                    Description = r.Description,
                    Category = r.Category,
                    Tags = r.Tags,
                    IsEncrypted = r.IsEncrypted
                });
            }

            // パッケージ全体を JSON にシリアライズし、UTF-8 でファイルに書き出す（インデント付き）
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(pkg, CachedJsonSerializerOptions),
                Encoding.UTF8);
        }

        /// <summary>
        /// エクスポートファイルからすべての <see cref="Credential"/> を復元し、
        /// 現在のデータベースへインポートします。
        /// マスターパスワードが設定されている場合は <see cref="AdvancedEncryptionService"/> を使用して fileKey を復号し、
        /// 未設定の場合はユーザー指定のインポート用パスワードで fileKey を復号します。
        /// </summary>
        /// <param name="path">インポート対象のエクスポートファイルのフルパス。</param>
        /// <param name="importPassword">
        /// マスターパスワードが未設定の場合に fileKey を復号するために使用されるパスワード。
        /// マスターパスワードが設定されている場合は無視されます。
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// パッケージが不正な形式の場合、またはマスターパスワード未設定かつ <paramref name="importPassword"/> が null または空文字の場合にスローされます。
        /// </exception>
        /// <exception cref="CryptographicException">
        /// パスワード不一致やデータ破損などにより暗号復号に失敗した場合にスローされる可能性があります。
        /// </exception>
        public static void ImportAll(string path, string? importPassword = null)
        {
            // ファイルから JSON 文字列を読み込み
            var json = File.ReadAllText(path, Encoding.UTF8);

            // JSON を ExportPackage としてデシリアライズ（失敗した場合は InvalidOperationException）
            var pkg = JsonSerializer.Deserialize<ExportPackage>(json, CachedJsonSerializerOptions) ?? throw new InvalidOperationException("Invalid package");

            // ラップ済み fileKey を Base64 からバイト配列へ復元
            var wrapped = Convert.FromBase64String(pkg.WrappedFileKeyBase64);

            byte[] fileKey;

            // マスターパスワードが設定されている場合は AdvancedEncryptionService で解包
            if (AdvancedEncryptionService.IsMasterSet)
            {
                // ラップされた fileKey データを UTF-8 文字列として取得
                var wrappedStr = Encoding.UTF8.GetString(wrapped);

                // 文字列を AdvancedEncryptionService で復号して Base64 の fileKey を取り出す
                var b64 = AdvancedEncryptionService.DecryptString(wrappedStr);

                // fileKey をバイト列に戻す
                fileKey = Convert.FromBase64String(b64);
            }
            else
            {
                // マスターパスワードがない場合は importPassword による復号が必要
                if (string.IsNullOrEmpty(importPassword))
                {
                    throw new InvalidOperationException("Import password required");
                }

                // wrapped は salt + iv + cipher + tag の構造
                var salt = wrapped.Take(16).ToArray();
                var iv = wrapped.Skip(16).Take(12).ToArray();
                var tag = wrapped.Skip(wrapped.Length - 16).Take(16).ToArray();
                var cipher = wrapped.Skip(16 + iv.Length)
                                    .Take(wrapped.Length - 16 - iv.Length - tag.Length)
                                    .ToArray();

                // PBKDF2 でインポートパスワードから鍵を導出
                using var derive = new Rfc2898DeriveBytes(importPassword, salt, 200_000, HashAlgorithmName.SHA256);
                var key = derive.GetBytes(32);

                // 復号された fileKey 用のバッファを確保
                fileKey = new byte[cipher.Length];

                // AES-GCM で fileKey を復号
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(iv, cipher, tag, fileKey);
            }

            // 復号した fileKey を用いて各 Record のパスワードを復号し、Credential として DB に保存
            var svc = new CredentialService();

            foreach (var rec in pkg.Records)
            {
                // payload は iv + cipher + tag を Base64 化したもの
                var payload = Convert.FromBase64String(rec.Password);

                // iv / tag / cipher を分割
                var iv = payload.Take(12).ToArray();
                var tag = payload.Skip(payload.Length - 16).Take(16).ToArray();
                var cipher = payload.Skip(12).Take(payload.Length - 12 - 16).ToArray();

                // 平文パスワード用バッファ
                var plain = new byte[cipher.Length];

                // fileKey を使用してパスワードを復号
                using (var aes = new AesGcm(fileKey, 16))
                {
                    aes.Decrypt(iv, cipher, tag, plain);
                }

                // UTF-8 文字列としてパスワードを復元
                var pwd = Encoding.UTF8.GetString(plain);

                // 新しい Credential を構築（ここでは DB 保存時は平文で保存し、暗号化フラグも元データを引き継ぐ）
                var c = new Models.Credential
                {
                    LoginId = rec.LoginId,
                    Password = pwd,
                    Description = rec.Description,
                    Category = rec.Category,
                    Tags = rec.Tags,
                    IsEncrypted = rec.IsEncrypted
                };

                // DB に追加（ここでは encryptPassword: false として平文保存）
                svc.Add(c, encryptPassword: false);
            }
        }
    }
}
