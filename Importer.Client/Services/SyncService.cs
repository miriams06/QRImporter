using Importer.Client.Models;

namespace Importer.Client.Services;

public sealed class SyncService
{
    private readonly IndexedDbService _db;
    private readonly ApiClient _api;
    private readonly FeatureFlagsService _flags;
    private readonly OnlineModeService _online;

    private bool _running;

    public SyncService(
        IndexedDbService db,
        ApiClient api,
        FeatureFlagsService flags,
        OnlineModeService online)
    {
        _db = db;
        _api = api;
        _flags = flags;
        _online = online;
    }

    public async Task SyncPendingAsync()
    {
        if (_running) return;
        _running = true;

        try
        {
            if (!_flags.EffectiveServerSyncEnabled)
                return;

            if (!await _online.IsEffectivelyOnlineAsync())
                return;

            var due = await _db.GetOutboxDueAsync(DateTimeOffset.UtcNow);

            foreach (var item in due)
            {
                try
                {
                    await SyncOneAsync(item.DocumentId);

                    await _db.UpdateSyncStatusAsync(item.DocumentId, SyncStatus.Synced);
                }
                catch (Exception ex)
                {
                    var updated = ComputeRetry(item, ex.Message);

                    await _db.UpdateSyncStatusAsync(item.DocumentId, SyncStatus.Error, ex.Message);
                    await _db.UpsertOutboxAsync(updated);
                }
            }
        }
        finally
        {
            _running = false;
        }
    }

    private async Task SyncOneAsync(Guid documentId)
    {
        var doc = await _db.GetDocumentAsync(documentId)
                  ?? throw new InvalidOperationException("Documento local não encontrado.");

        if (string.IsNullOrWhiteSpace(doc.FileKey))
            throw new InvalidOperationException("Documento sem FileKey (ficheiro não associado).");

        // MVP: metadata = JSON do LocalDocument (sem o JsonDocument em bruto)
        var metadataJson = SerializeMetadata(doc);

        var file = await _db.GetFileAsync(doc.FileKey)
           ?? throw new InvalidOperationException("Ficheiro local não encontrado.");

        var (fileName, contentType, bytes) = file;

        await _api.UploadDocumentAsync(metadataJson, fileName, contentType, bytes);

    }

    private static string SerializeMetadata(LocalDocument doc)
    {
        // Não enviar JsonDocument diretamente; envia string raw
        var qrParsedJson = doc.QrParsedData?.RootElement.GetRawText();

        var payload = new
        {
            doc.Id,
            doc.CreatedAt,
            doc.Source,
            doc.DocumentType,
            doc.Series,
            doc.DocumentNumber,
            doc.DocumentDate,
            doc.IssuerTaxId,
            doc.IssuerName,
            doc.IssuerAddress,
            doc.ATCUD,
            doc.QrRawPayload,
            QrParsedJson = qrParsedJson,
            doc.SyncStatus
        };

        return System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
    }

    private static OutboxItem ComputeRetry(OutboxItem item, string error)
    {
        var attempts = item.AttemptCount + 1;

        var delay = attempts switch
        {
            1 => TimeSpan.FromSeconds(5),
            2 => TimeSpan.FromSeconds(15),
            3 => TimeSpan.FromSeconds(30),
            4 => TimeSpan.FromMinutes(1),
            5 => TimeSpan.FromMinutes(2),
            _ => TimeSpan.FromMinutes(5),
        };

        var permanent = attempts >= 10;

        return new OutboxItem
        {
            DocumentId = item.DocumentId,
            AttemptCount = attempts,
            NextAttemptAt = DateTimeOffset.UtcNow.Add(delay),
            LastError = error,
            IsPermanentError = permanent
        };
    }
}