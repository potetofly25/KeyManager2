using Microsoft.EntityFrameworkCore;
using potetofly25.KeyManager2.Data;
using potetofly25.KeyManager2.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace potetofly25.KeyManager2.Services
{
    /// <summary>
    /// <see cref="Credential"/> エンティティに対する永続化処理をまとめたサービスクラスです。
    /// 追加・更新・削除および取得のロジックをカプセル化し、
    /// 必要に応じて <see cref="AdvancedEncryptionService"/> を用いた暗号化・復号を行います。
    /// </summary>
    public class CredentialService
    {
        /// <summary>
        /// <see cref="CredentialService"/> の新しいインスタンスを初期化します。
        /// コンストラクタ内でデータベースの存在確認と作成（必要に応じて）を行います。
        /// </summary>
        public CredentialService()
        {
            // データベースが存在しなければ作成する（テーブルも含む）
            using KeyManagerDbContext db = new();
            db.Database.EnsureCreated();
        }

        /// <summary>
        /// すべての <see cref="Credential"/> レコードを取得します。
        /// オプションとして、マスターパスワードが設定済みで暗号化フラグが立っているものを復号して返却します。
        /// </summary>
        /// <param name="tryDecrypt">
        /// true の場合、<see cref="AdvancedEncryptionService.IsMasterSet"/> が true であれば暗号化されたパスワードを復号して返します。
        /// false の場合、パスワードは暗号化されたまま返却されます。
        /// </param>
        /// <returns>全ての <see cref="Credential"/> レコードのリスト。</returns>
        public List<Credential> GetAll(bool tryDecrypt = true)
        {
            using KeyManagerDbContext db = new();

            // トラッキングなしで全 Credential を ID 順に取得
            var list = db.Credentials.AsNoTracking().OrderBy(c => c.Id).ToList();

            // 復号を試みる条件が満たされている場合のみ処理
            if (tryDecrypt && AdvancedEncryptionService.IsMasterSet)
            {
                foreach (var c in list)
                {
                    // 暗号化済みフラグが立っているものだけを復号
                    if (c.IsEncrypted)
                    {
                        try
                        {
                            c.Password = AdvancedEncryptionService.DecryptString(c.Password);
                        }
                        catch
                        {
                            // 復号に失敗した場合は例外を握りつぶし、そのまま暗号化文字列を残す
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 新しい <see cref="Credential"/> を追加します。
        /// パラメータに応じて、追加前にパスワードを暗号化することができます。
        /// </summary>
        /// <param name="c">追加対象の <see cref="Credential"/> インスタンス。</param>
        /// <param name="encryptPassword">
        /// true の場合、<see cref="AdvancedEncryptionService"/> を用いてパスワードを暗号化し、
        /// <see cref="Credential.IsEncrypted"/> を true に設定してから保存します。
        /// false の場合、パスワードは平文のまま保存され、<see cref="Credential.IsEncrypted"/> は変更されません。
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="encryptPassword"/> が true かつ <see cref="AdvancedEncryptionService.IsMasterSet"/> が false の場合にスローされます。
        /// </exception>
        public void Add(Credential c, bool encryptPassword = false)
        {
            using KeyManagerDbContext db = new();

            // 暗号化指定がある場合は、マスター設定状態を確認してから暗号化を実施
            if (encryptPassword)
            {
                if (!AdvancedEncryptionService.IsMasterSet)
                {
                    throw new InvalidOperationException("Master not set");
                }

                // パスワードを暗号化してフラグを設定
                c.Password = AdvancedEncryptionService.EncryptString(c.Password);
                c.IsEncrypted = true;
            }

            // 新規エンティティとして追加
            db.Credentials.Add(c);

            // 変更をデータベースへ反映
            db.SaveChanges();
        }

        /// <summary>
        /// 既存の <see cref="Credential"/> を更新します。
        /// パラメータに応じて、更新時にパスワードを暗号化または平文として扱います。
        /// </summary>
        /// <param name="c">更新対象の <see cref="Credential"/> インスタンス。</param>
        /// <param name="encryptPassword">
        /// true の場合、パスワードを暗号化し <see cref="Credential.IsEncrypted"/> を true に設定します。
        /// false の場合、パスワードを平文として扱い <see cref="Credential.IsEncrypted"/> を false に設定します。
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="encryptPassword"/> が true かつ <see cref="AdvancedEncryptionService.IsMasterSet"/> が false の場合にスローされます。
        /// </exception>
        public void Update(Credential c, bool encryptPassword = false)
        {
            using KeyManagerDbContext db = new();

            // 暗号化要求がある場合は、マスター設定を確認
            if (encryptPassword)
            {
                if (!AdvancedEncryptionService.IsMasterSet)
                {
                    throw new InvalidOperationException("Master not set");
                }

                // パスワードを暗号化し、暗号化済みフラグを立てる
                c.Password = AdvancedEncryptionService.EncryptString(c.Password);
                c.IsEncrypted = true;
            }
            else
            {
                // 暗号化しない場合は暗号化フラグをオフにする（平文として扱う）
                c.IsEncrypted = false;
            }

            // 既存エンティティとして更新
            db.Credentials.Update(c);

            // 変更をデータベースへ反映
            db.SaveChanges();
        }

        /// <summary>
        /// 指定された <see cref="Credential"/> レコードを削除します。
        /// </summary>
        /// <param name="c">削除対象の <see cref="Credential"/> インスタンス。</param>
        public void Delete(Credential c)
        {
            using KeyManagerDbContext db = new();

            // 指定エンティティを削除状態としてマーク
            db.Credentials.Remove(c);

            // 変更をデータベースへ反映
            db.SaveChanges();
        }
    }
}
