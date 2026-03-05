namespace Importer.Api.Entities;

public class AuditLogEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? DocumentId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;

    public string Actor { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;

    public string DetailsJson { get; set; } = string.Empty;
}