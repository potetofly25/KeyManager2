using System.Collections.Generic;

namespace potetofly25.KeyManager2.Models
{
    /// <summary>
    /// エクスポートデータ全体を表すパッケージモデル。
    /// バージョン情報、暗号化された鍵、エクスポート対象のレコード群を保持します。
    /// 他環境やバックアップファイルとしての入出力に使用されます。
    /// </summary>
    public class ExportPackage
    {
        /// <summary>
        /// エクスポートパッケージのバージョン番号。
        /// データ構造変更時の互換性判断に利用します。
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// レコード復号のための鍵を Base64 形式でラップした文字列。
        /// バックアップファイル内で使用される暗号鍵の安全な転送を目的とします。
        /// </summary>
        public string WrappedFileKeyBase64 { get; set; } = string.Empty;

        /// <summary>
        /// エクスポート対象のレコード一覧。
        /// 個々の Record モデルに認証情報、メタデータ、暗号化データなどが含まれます。
        /// </summary>
        public List<Record> Records { get; set; } = [];
    }
}
