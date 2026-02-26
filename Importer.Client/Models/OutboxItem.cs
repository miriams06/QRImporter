namespace Importer.Client.Models;

public sealed class OutboxItem
{
    public Guid DocumentId { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LastError { get; set; }
    public bool IsPermanentError { get; set; }
}
