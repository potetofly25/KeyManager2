using System.ComponentModel.DataAnnotations;

namespace potetofly25.KeyManager2.Models
{
    /// <summary>
    /// 認証情報（ログインID、パスワード、説明、分類など）を表すモデルクラス。
    /// このエンティティは SQLite データベースの Credential テーブルへマッピングされます。
    /// </summary>
    public class Credential
    {
        /// <summary>
        /// 主キーを表す識別子。
        /// 自動的にインクリメントされる一意の整数値です。
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 認証に使用するログインID。
        /// Web サイト、アプリケーション、サービスなどで利用されるアカウント名を格納します。
        /// </summary>
        public string LoginId { get; set; } = string.Empty;

        /// <summary>
        /// 認証に使用するパスワード。
        /// 必要に応じて暗号化されて保存されます。
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 認証情報に関する説明文。
        /// メモや用途などの自由記述欄として使用できます。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 認証情報の分類カテゴリ。
        /// Web サービス、アプリ、業務、個人利用など任意の用途に分類するために利用します。
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 認証情報に付与するタグ一覧。
        /// カンマ区切りで複数タグを指定できます。
        /// 例: "仕事,銀行,個人"
        /// </summary>
        public string? Tags { get; set; } // カンマ区切りで複数タグを格納

        /// <summary>
        /// パスワードなどのデータが暗号化されているかどうかのフラグ。
        /// 暗号化済みの場合は true、平文の場合は false を示します。
        /// </summary>
        public bool IsEncrypted { get; set; } = false;
    }
}
