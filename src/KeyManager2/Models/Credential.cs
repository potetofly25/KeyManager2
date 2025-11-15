using System.ComponentModel.DataAnnotations;

namespace potetofly25.KeyManager2.Models
{
    public class Credential
    {
        [Key]
        public int Id { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Tags { get; set; } // comma separated
        public bool IsEncrypted { get; set; } = false;
    }
}
