namespace potetofly25.KeyManager2.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // encrypted with fileKey (base64)
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Tags { get; set; }
        public bool IsEncrypted { get; set; }
    }

}
