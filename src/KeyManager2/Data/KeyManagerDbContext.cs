using Microsoft.EntityFrameworkCore;
using potetofly25.KeyManager2.Models;
using System.IO;

namespace potetofly25.KeyManager2.Data
{
    public class KeyManagerDbContext : DbContext
    {
        public DbSet<Credential> Credentials { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "KeyManager2.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
