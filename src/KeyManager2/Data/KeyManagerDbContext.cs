using Microsoft.EntityFrameworkCore;
using potetofly25.KeyManager2.Models;
using System.IO;

namespace potetofly25.KeyManager2.Data
{
    /// <summary>
    /// データベース操作を司る EF Core の DbContext クラス。
    /// 本アプリケーションにおけるすべてのエンティティ・モデルをこのコンテキストで管理します。
    /// </summary>
    public class KeyManagerDbContext : DbContext
    {
        /// <summary>
        /// Credential テーブルを表す DbSet。
        /// 認証情報（ID、パスワード、説明など）を格納および検索するために使用します。
        /// </summary>
        public DbSet<Credential> Credentials { get; set; } = null!;

        /// <summary>
        /// データベース接続設定を行うメソッド。
        /// SQLite のファイルパスを生成し、DbContext の接続先として構成します。
        /// </summary>
        /// <param name="optionsBuilder">DbContextOptionsBuilder インスタンス</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "KeyManager2.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
