using System.Collections.Generic;

namespace potetofly25.KeyManager2.Models
{
    public class ExportPackage
    {
        public int Version { get; set; } = 1;
        public string WrappedFileKeyBase64 { get; set; } = string.Empty;
        public List<Record> Records { get; set; } = [];
    }
}
