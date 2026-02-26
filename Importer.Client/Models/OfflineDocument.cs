namespace Importer.Client.Models
{
    /// <summary>
    /// Modelo de documento armazenado localmente (offline-first).
    /// </summary>
    public class OfflineDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public byte[]? FileContent { get; set; }

        public string JsonData { get; set; } = string.Empty;

        public string Status { get; set; } = "Draft"; // Draft | PendingSync | Error

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
