namespace Importer.Client.Models
{
    public sealed class QrResult
    {
        public string? Payload { get; set; }
        public string Status { get; set; } = "NOT_FOUND";
        public string Strategy { get; set; } = "none";
        public int Attempts { get; set; }
        public long DurationMs { get; set; }
        public int? PageNumber { get; set; }

        public bool Success => string.Equals(Status, "OK", System.StringComparison.OrdinalIgnoreCase)
                               && !string.IsNullOrWhiteSpace(Payload);
    }
}
