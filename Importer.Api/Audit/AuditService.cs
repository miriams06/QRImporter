using System.Text.Json;
using Importer.Api.Data;
using Importer.Api.Entities;

namespace Importer.Api.Audit;

public sealed class AuditService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AppDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task TryLogAsync(
        string eventType,
        string outcome,
        Guid? documentId,
        string actor,
        string source,
        object? details,
        CancellationToken ct = default)
    {
        try
        {
            var detailsJson = details is null
                ? string.Empty
                : JsonSerializer.Serialize(details, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            _db.AuditLogs.Add(new AuditLogEntity
            {
                DocumentId = documentId,
                EventType = eventType,
                Outcome = outcome,
                Actor = actor,
                Source = source,
                DetailsJson = detailsJson
            });

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha a persistir audit log: eventType={EventType} documentId={DocumentId}", eventType, documentId);
        }
    }
}