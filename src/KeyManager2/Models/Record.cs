namespace potetofly25.KeyManager2.Models
{
    /// <summary>
    /// エクスポート／インポート処理で使用されるレコードデータモデル。
    /// Credential の内容をファイル鍵で暗号化した形式で保持します。
    /// </summary>
    public class Record
    {
        /// <summary>
        /// 一意の識別子。インポート／エクスポート時の整合性維持に利用されます。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 認証情報として使用するログインID。
        /// 通常は平文で格納されます。
        /// </summary>
        public string LoginId { get; set; } = string.Empty;

        /// <summary>
        /// 認証に使用するパスワード。
        /// エクスポート時に fileKey を用いて暗号化された Base64 文字列が格納されます。
        /// </summary>
        public string Password { get; set; } = string.Empty; // fileKey で暗号化された Base64 文字列

        /// <summary>
        /// 任意の説明文。
        /// サイト情報や用途メモなど、付随情報を格納します。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// カテゴリ名（任意）。
        /// 種別や分類を設定する際に使用します。
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// タグ一覧。カンマ区切り形式で複数タグを保持します。
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// パスワードが暗号化されているかどうかを示すフラグ。
        /// エクスポート時は通常 true となります。
        /// </summary>
        public bool IsEncrypted { get; set; }
    }
}
