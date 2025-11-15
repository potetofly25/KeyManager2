using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace potetofly25.KeyManager2.Services
{
    /// <summary>
    /// マスターパスワードを用いた高度な暗号化・復号を提供するサービスクラスです。
    /// ルート鍵（rootKey）をマスターパスワードでラップして安全に保存し、
    /// そのルート鍵から派生させたサブキーでアプリ内の秘密情報を暗号化・復号します。
    /// </summary>
    public static class AdvancedEncryptionService
    {
        /// <summary>
        /// 暗号鍵のバイト長（32 バイト = 256bit）を表します。
        /// </summary>
        private const int KeyBytes = 32;

        /// <summary>
        /// パスワード導出に用いるソルトのバイト長（32 バイト）を表します。
        /// </summary>
        private const int SaltBytes = 32;

        /// <summary>
        /// PBKDF2 による鍵導出の反復回数です。高い反復回数により総当たり攻撃に対して強くします。
        /// </summary>
        private const int Iterations = 200_000;

        /// <summary>
        /// AES-GCM が使用する認証タグの長さ（16 バイト）を表します。
        /// </summary>
        private const int GcmTagLength = 16;

        /// <summary>
        /// パスワード導出用ソルトを保存するファイルパスです。
        /// </summary>
        private static readonly string SaltFile = Path.Combine(Directory.GetCurrentDirectory(), "KeyManager2_adv.salt");

        /// <summary>
        /// マスターパスワードでラップされたルート鍵を保存するファイルパスです。
        /// </summary>
        private static readonly string WrappedRootKeyFile = Path.Combine(Directory.GetCurrentDirectory(), "KeyManager2_root.wrapped");

        /// <summary>
        /// メモリ上に展開されたルート鍵を保持するフィールドです。
        /// マスターパスワードが設定済みかどうかの判定にも使用されます。
        /// </summary>
        private static byte[]? _rootKey;

        /// <summary>
        /// マスターパスワードが設定・展開され、ルート鍵がメモリ上に存在するかどうかを示すフラグです。
        /// </summary>
        public static bool IsMasterSet => _rootKey != null;

        /// <summary>
        /// ソルト（パスワードから鍵を導出する際に使用するランダムバイト列）を確保します。
        /// 既存のソルトファイルがある場合はそこから読み込み、なければ新たに生成・保存します。
        /// </summary>
        /// <returns>ソルトのバイト配列。</returns>
        private static byte[] EnsureSalt()
        {
            // 既にソルトファイルが存在する場合は再利用する
            if (File.Exists(SaltFile))
            {
                return File.ReadAllBytes(SaltFile);
            }

            // 新たにソルトを生成
            var s = new byte[SaltBytes];
            RandomNumberGenerator.Fill(s);

            // ソルトをファイルに保存
            File.WriteAllBytes(SaltFile, s);

            return s;
        }

        /// <summary>
        /// 初回セットアップ用にマスターパスワードを使用して新しいルート鍵を生成し、
        /// そのルート鍵をパスワードでラップして永続化します。
        /// Windows 環境では DPAPI (ProtectedData) による追加保護も試みます。
        /// </summary>
        /// <param name="masterPassword">マスターパスワードとなる文字列。</param>
        public static void InitializeMasterPassword(string masterPassword)
        {
            // ルート鍵を生成（完全ランダムな 32 バイト）
            var root = new byte[KeyBytes];
            RandomNumberGenerator.Fill(root);

            // ルート鍵をマスターパスワードでラップ（暗号化）する
            var wrapped = WrapRootKeyWithPassword(root, masterPassword);

            // プラットフォーム固有保護（Windows の CurrentUser スコープ）を試行
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    // DPAPI による追加保護
                    var protectedBytes = ProtectedData.Protect(wrapped, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(WrappedRootKeyFile, protectedBytes);
                    _rootKey = root;
                    return;
                }
            }
            catch
            {
                // 失敗した場合は DPAPI 保護なしで保存する（フォールバック）
            }

            // 標準的なラップデータをそのまま保存
            File.WriteAllBytes(WrappedRootKeyFile, wrapped);

            // メモリ上にルート鍵を保持
            _rootKey = root;
        }

        /// <summary>
        /// 既存のラップ済みルート鍵をマスターパスワードで復号し、
        /// メモリ上にルート鍵を展開します。
        /// </summary>
        /// <param name="masterPassword">保存時と同じマスターパスワード。</param>
        /// <exception cref="InvalidOperationException">ラップされたルート鍵ファイルが存在しない場合にスローされます。</exception>
        /// <exception cref="CryptographicException">パスワードが誤っているなどで復号に失敗した場合にスローされます。</exception>
        public static void SetMasterPassword(string masterPassword)
        {
            // ラップ済みルート鍵ファイルがない場合は初期化されていない
            if (!File.Exists(WrappedRootKeyFile))
            {
                throw new InvalidOperationException("No wrapped root key stored.");
            }

            // ファイルからラップ済みデータを読み込み
            var wrapped = File.ReadAllBytes(WrappedRootKeyFile);

            // Windows 環境で DPAPI 保護されていれば解除を試みる
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var unprotected = ProtectedData.Unprotect(wrapped, null, DataProtectionScope.CurrentUser);
                    wrapped = unprotected;
                }
            }
            catch
            {
                // DPAPI 解除に失敗しても、そのまま wrapped を用いて復号を試みる
            }

            // マスターパスワードでルート鍵を復号
            var root = UnwrapRootKeyWithPassword(wrapped, masterPassword);

            // 復号に成功したルート鍵をメモリ上に保持
            _rootKey = root;
        }

        /// <summary>
        /// メモリ上に保持しているルート鍵を破棄し、マスターパスワードの状態をクリアします。
        /// 実際のファイルは削除せず、再度 SetMasterPassword を呼ぶことで復元可能です。
        /// </summary>
        public static void ClearMasterPassword()
        {
            // ルート鍵が確保済みならゼロクリアしてから参照を破棄
            if (_rootKey != null)
            {
                Array.Clear(_rootKey, 0, _rootKey.Length);
                _rootKey = null;
            }
        }

        /// <summary>
        /// ルート鍵をマスターパスワードでラップ（暗号化）します。
        /// PBKDF2 によって導出された鍵を使用し、AES-GCM でルート鍵を保護します。
        /// </summary>
        /// <param name="rootKey">ラップ対象のルート鍵バイト列。</param>
        /// <param name="password">マスターパスワード。</param>
        /// <returns>ソルト、IV、暗号化データ、タグを連結したラップ済みバイト列。</returns>
        private static byte[] WrapRootKeyWithPassword(byte[] rootKey, string password)
        {
            // ソルトを取得（既存 or 新規）
            var salt = EnsureSalt();

            // PBKDF2 によりパスワードから 32 バイトの鍵を導出
            using var derive = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeyBytes);

            // AES-GCM 用 IV（12 バイト）を生成
            var iv = RandomNumberGenerator.GetBytes(12);

            // 暗号文とタグのバッファを確保
            var cipher = new byte[rootKey.Length];
            var tag = new byte[GcmTagLength];

            // AES-GCM でルート鍵を暗号化
            using (var aes = new AesGcm(key, GcmTagLength))
            {
                aes.Encrypt(iv, rootKey, cipher, tag);
            }

            // ソルト + IV + 暗号データ + タグを連結してラップ済みデータを構成
            var wrapped = new byte[salt.Length + iv.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(salt, 0, wrapped, 0, salt.Length);
            Buffer.BlockCopy(iv, 0, wrapped, salt.Length, iv.Length);
            Buffer.BlockCopy(cipher, 0, wrapped, salt.Length + iv.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, wrapped, salt.Length + iv.Length + cipher.Length, tag.Length);

            return wrapped;
        }

        /// <summary>
        /// マスターパスワードを用いてラップ済みルート鍵を復号します。
        /// ラップ時に連結されたソルト、IV、暗号データ、タグから元のルート鍵を取り出します。
        /// </summary>
        /// <param name="wrapped">ソルト、IV、暗号データ、タグが連結されたラップ済みデータ。</param>
        /// <param name="password">マスターパスワード。</param>
        /// <returns>復号されたルート鍵のバイト配列。</returns>
        /// <exception cref="CryptographicException">パスワード不一致などで復号に失敗した場合にスローされます。</exception>
        private static byte[] UnwrapRootKeyWithPassword(byte[] wrapped, string password)
        {
            // 先頭からソルト、IV、末尾からタグを取り出し、残りを暗号データとみなす
            var salt = wrapped.Take(SaltBytes).ToArray();
            var iv = wrapped.Skip(SaltBytes).Take(12).ToArray();
            var tag = wrapped.Skip(wrapped.Length - GcmTagLength).Take(GcmTagLength).ToArray();
            var cipher = wrapped.Skip(SaltBytes + iv.Length)
                                .Take(wrapped.Length - SaltBytes - iv.Length - tag.Length)
                                .ToArray();

            // PBKDF2 によりパスワードから鍵を導出
            using var derive = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeyBytes);

            // 復号先バッファを準備
            var root = new byte[cipher.Length];

            // AES-GCM による復号
            using (var aes = new AesGcm(key, GcmTagLength))
            {
                aes.Decrypt(iv, cipher, tag, root);
            }

            return root;
        }

        /// <summary>
        /// ルート鍵から暗号化用サブキーと HMAC 用サブキーを派生させます。
        /// HMAC-SHA256 を利用し、それぞれ異なる info 値（"enc", "hmac"）を用いた HKDF 風の処理です。
        /// </summary>
        /// <param name="root">サブキーの元となるルート鍵。</param>
        /// <returns>暗号化用鍵と HMAC 用鍵を含むタプル。</returns>
        private static (byte[] encKey, byte[] hmacKey) DeriveSubKeys(byte[] root)
        {
            // 内部ローカル関数：指定された info を用いて HMAC-SHA256 を計算し、32 バイトをサブキーとする
            byte[] Derive(byte[] info)
            {
                using var hmac = new HMACSHA256(root);
                return [.. hmac.ComputeHash(info).Take(KeyBytes)];
            }

            // 暗号化用サブキー（info="enc"）
            var encKey = Derive(Encoding.UTF8.GetBytes("enc"));

            // HMAC 用サブキー（info="hmac"）
            var hmacKey = Derive(Encoding.UTF8.GetBytes("hmac"));

            return (encKey, hmacKey);
        }

        /// <summary>
        /// 現在設定されているマスターパスワード（ルート鍵）を使用して、
        /// 指定された平文文字列を暗号化し、Base64 文字列として返します。
        /// AES-GCM による認証付き暗号と HMAC による完全性検証用タグを組み合わせています。
        /// </summary>
        /// <param name="plain">暗号化したい平文文字列。</param>
        /// <returns>暗号化されたデータを表す Base64 文字列。</returns>
        /// <exception cref="InvalidOperationException">マスターパスワード（ルート鍵）が未設定の場合にスローされます。</exception>
        public static string EncryptString(string plain)
        {
            // ルート鍵がメモリ上に存在しない場合は、マスターパスワードが設定されていない
            if (_rootKey == null)
            {
                throw new InvalidOperationException("Master not set.");
            }

            // ルート鍵から暗号化用鍵と HMAC 用鍵を派生
            var (encKey, hmacKey) = DeriveSubKeys(_rootKey);

            // 平文を UTF-8 バイト列へ変換
            var plainBytes = Encoding.UTF8.GetBytes(plain ?? string.Empty);

            // AES-GCM 用 IV を生成
            var iv = RandomNumberGenerator.GetBytes(12);

            // 暗号データおよびタグのバッファを準備
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[GcmTagLength];

            // AES-GCM による暗号化
            using (var aes = new AesGcm(encKey, GcmTagLength))
            {
                aes.Encrypt(iv, plainBytes, cipher, tag);
            }

            // iv + cipher + tag をペイロードとして連結
            var payload = new byte[iv.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, payload, iv.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, payload, iv.Length + cipher.Length, tag.Length);

            // ペイロードに対して HMAC を計算し、末尾に連結
            byte[] hmac;
            using (var mac = new HMACSHA256(hmacKey))
            {
                hmac = mac.ComputeHash(payload);
            }

            var full = new byte[payload.Length + hmac.Length];
            Buffer.BlockCopy(payload, 0, full, 0, payload.Length);
            Buffer.BlockCopy(hmac, 0, full, payload.Length, hmac.Length);

            // 完成したバイト列を Base64 文字列として返却
            return Convert.ToBase64String(full);
        }

        /// <summary>
        /// 現在設定されているマスターパスワード（ルート鍵）を使用して、
        /// EncryptString によって生成された Base64 文字列を復号し、元の平文文字列を返します。
        /// HMAC による完全性チェックと、AES-GCM の認証タグ検証を行います。
        /// </summary>
        /// <param name="b64">暗号化済みデータを表す Base64 文字列。</param>
        /// <returns>復号された平文文字列。</returns>
        /// <exception cref="InvalidOperationException">マスターパスワード（ルート鍵）が未設定の場合にスローされます。</exception>
        /// <exception cref="CryptographicException">
        /// データ形式が不正、HMAC 検証失敗、AES-GCM の復号失敗など、暗号的な整合性が取れない場合にスローされます。
        /// </exception>
        public static string DecryptString(string b64)
        {
            // ルート鍵が未設定の場合は復号不可
            if (_rootKey == null)
            {
                throw new InvalidOperationException("Master not set.");
            }

            // ルート鍵からサブキーを派生
            var (encKey, hmacKey) = DeriveSubKeys(_rootKey);

            // Base64 文字列をバイト配列へ変換
            var full = Convert.FromBase64String(b64);

            // 最低限の長さチェック（IV + TAG + HMAC 長を満たしているか）
            if (full.Length < 12 + GcmTagLength + 32)
            {
                throw new CryptographicException("Invalid payload");
            }

            // 末尾 32 バイトを HMAC、残りをペイロードとみなす
            var hmac = full.Skip(full.Length - 32).Take(32).ToArray();
            var payload = full.Take(full.Length - 32).ToArray();

            // HMAC による完全性チェック
            using (var mac = new HMACSHA256(hmacKey))
            {
                var expected = mac.ComputeHash(payload);

                // FixedTimeEquals によるタイミング攻撃対策付き比較
                if (!CryptographicOperations.FixedTimeEquals(expected, hmac))
                {
                    throw new CryptographicException("HMAC mismatch");
                }
            }

            // ペイロードから IV、タグ、暗号データを分割
            var iv = payload.Take(12).ToArray();
            var tag = payload.Skip(payload.Length - GcmTagLength).Take(GcmTagLength).ToArray();
            var cipher = payload.Skip(12).Take(payload.Length - 12 - GcmTagLength).ToArray();

            // 復号用バッファ
            var plain = new byte[cipher.Length];

            // AES-GCM で復号
            using (var aes = new AesGcm(encKey, GcmTagLength))
            {
                aes.Decrypt(iv, cipher, tag, plain);
            }

            // UTF-8 文字列へ変換して返却
            return Encoding.UTF8.GetString(plain);
        }
    }
}
