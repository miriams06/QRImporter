using System.Text.Json;
using Importer.Client.Models;
using Microsoft.JSInterop;

namespace Importer.Client.Services;

public sealed class IndexedDbService
{
    private readonly IJSRuntime _js;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public IndexedDbService(IJSRuntime js) => _js = js;

    /// <summary>
    /// Guarda um documento e o ficheiro localmente (offline-first) e enfileira na outbox.
    /// </summary>
    public async Task SaveDocumentAsync(LocalDocument document, string fileName, string contentType, byte[] fileBytes)
    {
        // Garantir FileKey (vamos usar "doc:{id}" por consistência)
        if (string.IsNullOrWhiteSpace(document.FileKey))
            document.FileKey = $"doc:{document.Id:D}";

        // Ao guardar offline, o estado deve ser PendingSync
        if (document.SyncStatus == SyncStatus.Draft)
            document.SyncStatus = SyncStatus.PendingSync;

        // Serialização segura do QrParsedData
        var qrParsedJson = document.QrParsedData?.RootElement.GetRawText();

        // 1) Guardar documento
        await _js.InvokeVoidAsync("importerIndexedDb.put", "documents", new
        {
            id = document.Id.ToString("D"),
            createdAt = document.CreatedAt.ToString("O"),
            source = document.Source,
            documentType = document.DocumentType,
            series = document.Series,
            documentNumber = document.DocumentNumber,
            documentDate = document.DocumentDate.ToString("O"),
            issuerTaxId = document.IssuerTaxId,
            issuerName = document.IssuerName,
            atcud = document.ATCUD,
            qrRawPayload = document.QrRawPayload,
            qrParsedJson = qrParsedJson,
            syncStatus = (int)document.SyncStatus,
            fileKey = document.FileKey,
            errorMessage = document.ErrorMessage
        });

        // 2) Guardar ficheiro (Blob)
        await _js.InvokeVoidAsync("importerIndexedDb.putFile",
            document.FileKey,
            fileName,
            contentType,
            fileBytes);

        // 3) Enfileirar outbox (upsert)
        await _js.InvokeVoidAsync("importerIndexedDb.put", "outbox", new
        {
            documentId = document.Id.ToString("D"),
            attemptCount = 0,
            nextAttemptAt = DateTimeOffset.UtcNow.ToString("O"),
            lastError = (string?)null,
            isPermanentError = false
        });
    }

    /// <summary>
    /// Lê documentos pendentes de sincronização (por SyncStatus).
    /// (Útil para UI / histórico)
    /// </summary>
    public async Task<IEnumerable<LocalDocument>> GetPendingDocumentsAsync()
    {
        // MVP: ler todos e filtrar (mais tarde cria índice/s.
        var all = await _js.InvokeAsync<DocumentJs[]>("importerIndexedDb.getAll", "documents");
        return all
            .Select(Map)
            .Where(d => d.SyncStatus == SyncStatus.PendingSync)
            .ToList();
    }

    /// <summary>
    /// Lista itens da outbox cujo nextAttemptAt já expirou.
    /// (Para o SyncService)
    /// </summary>
    public async Task<IReadOnlyList<OutboxItem>> GetOutboxDueAsync(DateTimeOffset now)
    {
        var items = await _js.InvokeAsync<OutboxJs[]>("importerIndexedDb.listOutboxDue", now.ToString("O"));
        return items.Select(x => new OutboxItem
        {
            DocumentId = Guid.Parse(x.documentId),
            AttemptCount = x.attemptCount,
            NextAttemptAt = DateTimeOffset.Parse(x.nextAttemptAt),
            LastError = x.lastError,
            IsPermanentError = x.isPermanentError
        }).ToList();
    }


    /// <summary>
    /// Lista todos os itens da outbox (incluindo não-due e permanentes),
    /// para diagnóstico/visualização no histórico.
    /// </summary>
    public async Task<IReadOnlyList<OutboxItem>> GetAllOutboxAsync()
    {
        var items = await _js.InvokeAsync<OutboxJs[]>("importerIndexedDb.getAll", "outbox");
        return items.Select(x => new OutboxItem
        {
            DocumentId = Guid.Parse(x.documentId),
            AttemptCount = x.attemptCount,
            NextAttemptAt = DateTimeOffset.Parse(x.nextAttemptAt),
            LastError = x.lastError,
            IsPermanentError = x.isPermanentError
        }).ToList();
    }

    /// <summary>
    /// Força novo retry para um documento da outbox:
    /// - remove marcação de erro permanente,
    /// - agenda tentativa imediata.
    /// </summary>
    public async Task ForceRetryOutboxAsync(Guid documentId)
    {
        var dto = await _js.InvokeAsync<OutboxJs?>("importerIndexedDb.get", "outbox", documentId.ToString("D"));
        if (dto is null)
            return;

        dto.isPermanentError = false;
        dto.nextAttemptAt = DateTimeOffset.UtcNow.ToString("O");
        dto.lastError = null;

        await _js.InvokeVoidAsync("importerIndexedDb.put", "outbox", dto);
    }

    public async Task<IReadOnlyList<LocalDocument>> GetAllDocumentsAsync()
    {
        var all = await _js.InvokeAsync<DocumentJs[]>("importerIndexedDb.getAll", "documents");
        return all.Select(Map)
                  .OrderByDescending(d => d.CreatedAt)
                  .ToList();
    }

    public async Task<LocalDocument?> GetDocumentAsync(Guid id)
    {
        var dto = await _js.InvokeAsync<DocumentJs?>("importerIndexedDb.get", "documents", id.ToString("D"));
        return dto is null ? null : Map(dto);
    }

    public async Task<(string FileName, string ContentType, byte[] Bytes)?> GetFileAsync(string fileKey)
    {
        var dto = await _js.InvokeAsync<FileBase64Js?>("importerIndexedDb.getFileAsBase64", fileKey);
        if (dto is null) return null;

        var bytes = Convert.FromBase64String(dto.base64);
        return (dto.fileName, dto.contentType, bytes);
    }

    private sealed class FileBase64Js
    {
        public string fileName { get; set; } = "";
        public string contentType { get; set; } = "";
        public string base64 { get; set; } = "";
    }

    public sealed class FileInfoOnly
    {
        public string fileName { get; set; } = "";
        public string contentType { get; set; } = "";
    }

    public async Task<FileInfoOnly?> GetFileInfoAsync(string fileKey)
    {
        return await _js.InvokeAsync<FileInfoOnly?>("importerIndexedDb.getFileInfo", fileKey);
    }

    public async Task UpdateSyncStatusAsync(Guid documentId, SyncStatus status, string? errorMessage = null)
    {
        var doc = await GetDocumentAsync(documentId);
        if (doc is null) return;

        doc.SyncStatus = status;
        doc.ErrorMessage = errorMessage;

        // re-save document
        await SaveDocumentMetadataOnlyAsync(doc);

        // se synced, tira da outbox
        if (status == SyncStatus.Synced)
            await _js.InvokeVoidAsync("importerIndexedDb.del", "outbox", documentId.ToString("D"));
    }

    public async Task UpsertOutboxAsync(OutboxItem item)
    {
        await _js.InvokeVoidAsync("importerIndexedDb.put", "outbox", new
        {
            documentId = item.DocumentId.ToString("D"),
            attemptCount = item.AttemptCount,
            nextAttemptAt = item.NextAttemptAt.ToString("O"),
            lastError = item.LastError,
            isPermanentError = item.IsPermanentError
        });
    }

    private async Task SaveDocumentMetadataOnlyAsync(LocalDocument document)
    {
        var qrParsedJson = document.QrParsedData?.RootElement.GetRawText();

        await _js.InvokeVoidAsync("importerIndexedDb.put", "documents", new
        {
            id = document.Id.ToString("D"),
            createdAt = document.CreatedAt.ToString("O"),
            source = document.Source,
            documentType = document.DocumentType,
            series = document.Series,
            documentNumber = document.DocumentNumber,
            documentDate = document.DocumentDate.ToString("O"),
            issuerTaxId = document.IssuerTaxId,
            issuerName = document.IssuerName,
            atcud = document.ATCUD,
            qrRawPayload = document.QrRawPayload,
            qrParsedJson = qrParsedJson,
            syncStatus = (int)document.SyncStatus,
            fileKey = document.FileKey,
            errorMessage = document.ErrorMessage
        });
    }

    private static LocalDocument Map(DocumentJs dto)
    {
        return new LocalDocument
        {
            Id = Guid.Parse(dto.id),
            CreatedAt = DateTime.Parse(dto.createdAt, null, System.Globalization.DateTimeStyles.RoundtripKind),
            Source = dto.source ?? "",
            DocumentType = dto.documentType ?? "",
            Series = dto.series ?? "",
            DocumentNumber = dto.documentNumber ?? "",
            DocumentDate = DateTime.Parse(dto.documentDate ?? DateTime.UtcNow.ToString("O"), null, System.Globalization.DateTimeStyles.RoundtripKind),
            IssuerTaxId = dto.issuerTaxId ?? "",
            IssuerName = dto.issuerName ?? "",
            ATCUD = dto.atcud ?? "",
            QrRawPayload = dto.qrRawPayload ?? "",
            QrParsedData = string.IsNullOrWhiteSpace(dto.qrParsedJson) ? null : JsonDocument.Parse(dto.qrParsedJson),
            SyncStatus = (SyncStatus)dto.syncStatus,
            FileKey = dto.fileKey ?? "",
            ErrorMessage = dto.errorMessage
        };
    }

    // DTOs vindos do JS
    private sealed class OutboxJs
    {
        public string documentId { get; set; } = "";
        public int attemptCount { get; set; }
        public string nextAttemptAt { get; set; } = "";
        public string? lastError { get; set; }
        public bool isPermanentError { get; set; }
    }

    private sealed class DocumentJs
    {
        public string id { get; set; } = "";
        public string createdAt { get; set; } = "";
        public string? source { get; set; }
        public string? documentType { get; set; }
        public string? series { get; set; }
        public string? documentNumber { get; set; }
        public string? documentDate { get; set; }
        public string? issuerTaxId { get; set; }
        public string? issuerName { get; set; }
        public string? atcud { get; set; }
        public string? qrRawPayload { get; set; }
        public string? qrParsedJson { get; set; }
        public int syncStatus { get; set; }
        public string? fileKey { get; set; }
        public string? errorMessage { get; set; }
    }

    private sealed class FileJs
    {
        public string fileName { get; set; } = "";
        public string contentType { get; set; } = "";
        public object blob { get; set; } = default!;
    }
}
