namespace Importer.Api.Entities;

public class DocumentEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public string Source { get; set; } = "Upload";
    public string DocumentType { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }

    public string IssuerTaxId { get; set; } = string.Empty;
    public string IssuerName { get; set; } = string.Empty;
    public string IssuerAddress { get; set; } = string.Empty;
    public string Atcud { get; set; } = string.Empty;

    public string QrRawPayload { get; set; } = string.Empty;
    public string QrParsedJson { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = "PendingSync";

    public string StorageObjectKey { get; set; } = string.Empty;
    public string StorageMetadataKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }

    public string MetadataJson { get; set; } = string.Empty;
}
