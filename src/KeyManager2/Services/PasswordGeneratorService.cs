using System;
using System.Linq;
using System.Text;

namespace potetofly25.KeyManager2.Services
{
    /// <summary>
    /// ランダムなパスワード文字列を生成するサービスクラスです。
    /// 英大文字・英小文字・数字・記号の利用有無と長さを指定してパスワードを生成します。
    /// </summary>
    public class PasswordGeneratorService
    {
        /// <summary>
        /// 利用可能な英大文字の文字集合です。
        /// </summary>
        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// 利用可能な英小文字の文字集合です。
        /// </summary>
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// 利用可能な数字の文字集合です。
        /// </summary>
        private const string Digits = "0123456789";

        /// <summary>
        /// 利用可能な記号の文字集合です。
        /// </summary>
        private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>/?";

        /// <summary>
        /// 疑似乱数生成器。
        /// 一般的な用途では十分ですが、暗号論的に強い乱数ではありません。
        /// </summary>
        private readonly Random _rnd = new();

        /// <summary>
        /// 指定された長さと条件に基づき、ランダムなパスワードを生成します。
        /// </summary>
        /// <param name="length">生成するパスワードの長さ。</param>
        /// <param name="useUpper">英大文字を使用する場合は true。</param>
        /// <param name="useLower">英小文字を使用する場合は true。</param>
        /// <param name="useDigits">数字を使用する場合は true。</param>
        /// <param name="useSymbols">記号を使用する場合は true。</param>
        /// <returns>生成されたパスワード文字列。</returns>
        /// <remarks>
        /// すべてのフラグが false の場合は、フォールバックとして英小文字のみを使用して生成します。
        /// 暗号論的な強度が厳密に必要な場合は <see cref="System.Security.Cryptography.RandomNumberGenerator"/> の利用を検討してください。
        /// </remarks>
        public string Generate(int length, bool useUpper = true, bool useLower = true, bool useDigits = true, bool useSymbols = true)
        {
            // 使用する文字種をビルドするバッファ
            var pool = new StringBuilder();

            // 各フラグに応じて文字集合を追加
            if (useUpper) pool.Append(Upper);
            if (useLower) pool.Append(Lower);
            if (useDigits) pool.Append(Digits);
            if (useSymbols) pool.Append(Symbols);

            // 実際に利用する文字プール
            var p = pool.ToString();

            // すべてのフラグが false の場合は英小文字のみを使用するフォールバック
            if (string.IsNullOrEmpty(p)) p = Lower;

            // 指定された長さ分だけランダムに文字を選択してパスワードを生成
            return new string(
                [.. Enumerable.Range(0, length).Select(_ => p[_rnd.Next(p.Length)])]);
        }
    }
}
