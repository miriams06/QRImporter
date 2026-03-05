using System.Text.Json;
using Importer.Api.Data;
using Importer.Api.Entities;
using Importer.Api.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Importer.Api.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController : ControllerBase
{
    private readonly IStorageService _storage;
    private readonly AppDbContext _db;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IStorageService storage,
        AppDbContext db,
        ILogger<DocumentsController> logger)
    {
        _storage = storage;
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Metadata))
            return BadRequest("metadata é obrigatório.");

        if (form.File is null || form.File.Length == 0)
            return BadRequest("file é obrigatório.");

        if (!TryParseMetadata(form.Metadata, out var parsed, out var parseError))
            return BadRequest(parseError);

        var safeFileName = Path.GetFileName(form.File.FileName);
        var documentId = parsed.DocumentId;

        var objectKey = $"documents/{documentId:D}/{safeFileName}";
        var metaKey = $"documents/{documentId:D}/metadata.json";

        try
        {
            _logger.LogInformation(
                "Upload start: documentId={DocumentId} fileName={FileName} contentType={ContentType} size={Size} objectKey={ObjectKey}",
                documentId,
                safeFileName,
                form.File.ContentType,
                form.File.Length,
                objectKey);

            await using (var stream = form.File.OpenReadStream())
            {
                await _storage.PutAsync(
                    content: stream,
                    contentType: form.File.ContentType ?? "application/octet-stream",
                    objectKey: objectKey,
                    ct: ct);
            }

            var metaBytes = System.Text.Encoding.UTF8.GetBytes(form.Metadata);
            await using (var ms = new MemoryStream(metaBytes))
            {
                await _storage.PutAsync(
                    content: ms,
                    contentType: "application/json",
                    objectKey: metaKey,
                    ct: ct);
            }

            var entity = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
            if (entity is null)
            {
                entity = new DocumentEntity { Id = documentId };
                _db.Documents.Add(entity);
            }

            MapMetadata(entity, parsed, form, safeFileName, objectKey, metaKey);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Upload OK: documentId={DocumentId} objectKey={ObjectKey} metaKey={MetaKey}",
                documentId,
                objectKey,
                metaKey);

            return Ok(new
            {
                message = "Recebido com sucesso.",
                documentId,
                objectKey,
                metaKey,
                fileName = safeFileName,
                contentType = form.File.ContentType,
                size = form.File.Length
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Upload cancelado: documentId={DocumentId} objectKey={ObjectKey}",
                documentId,
                objectKey);

            return Problem(
                title: "Upload cancelado",
                detail: "O upload foi cancelado.",
                statusCode: 499);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Upload falhou: documentId={DocumentId} objectKey={ObjectKey} metaKey={MetaKey}",
                documentId,
                objectKey,
                metaKey);

            return Problem(
                title: "Erro a guardar documento",
                detail: ex.Message,
                statusCode: 502);
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DocumentsQuery query, CancellationToken ct)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var docs = _db.Documents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            docs = docs.Where(d => d.SyncStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Nif))
        {
            var nif = query.Nif.Trim();
            docs = docs.Where(d => d.IssuerTaxId.Contains(nif));
        }

        if (!string.IsNullOrWhiteSpace(query.Atcud))
        {
            var atcud = query.Atcud.Trim();
            docs = docs.Where(d => d.Atcud.Contains(atcud));
        }

        if (!string.IsNullOrWhiteSpace(query.SeriesNumber))
        {
            var seriesNumber = query.SeriesNumber.Trim();
            docs = docs.Where(d =>
                d.Series.Contains(seriesNumber) ||
                d.DocumentNumber.Contains(seriesNumber) ||
                (d.Series + "/" + d.DocumentNumber).Contains(seriesNumber));
        }

        if (query.FromDate is not null)
        {
            var from = NormalizeToUtc(query.FromDate.Value.Date);
            docs = docs.Where(d => (d.DocumentDate ?? d.CreatedAtUtc) >= from);
        }

        if (query.ToDate is not null)
        {
            var toExclusive = NormalizeToUtc(query.ToDate.Value.Date.AddDays(1));
            docs = docs.Where(d => (d.DocumentDate ?? d.CreatedAtUtc) < toExclusive);
        }

        var total = await docs.CountAsync(ct);

        var items = await docs
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentListItemResponse
            {
                Id = d.Id,
                CreatedAtUtc = d.CreatedAtUtc,
                Source = d.Source,
                DocumentType = d.DocumentType,
                Series = d.Series,
                DocumentNumber = d.DocumentNumber,
                DocumentDate = d.DocumentDate,
                IssuerTaxId = d.IssuerTaxId,
                IssuerName = d.IssuerName,
                Atcud = d.Atcud,
                SyncStatus = d.SyncStatus,
                FileName = d.OriginalFileName,
                FileContentType = d.FileContentType,
                FileSize = d.FileSize,
                StorageObjectKey = d.StorageObjectKey
            })
            .ToListAsync(ct);

        return Ok(new DocumentsPageResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    private static void MapMetadata(
        DocumentEntity entity,
        ParsedMetadata metadata,
        DocumentUploadForm form,
        string safeFileName,
        string objectKey,
        string metaKey)
    {
        entity.CreatedAtUtc = NormalizeToUtc(metadata.CreatedAtUtc);
        entity.ReceivedAtUtc = DateTime.UtcNow;
        entity.Source = metadata.Source;
        entity.DocumentType = metadata.DocumentType;
        entity.Series = metadata.Series;
        entity.DocumentNumber = metadata.DocumentNumber;
        entity.DocumentDate = NormalizeToUtc(metadata.DocumentDate);
        entity.IssuerTaxId = metadata.IssuerTaxId;
        entity.IssuerName = metadata.IssuerName;
        entity.Atcud = metadata.Atcud;
        entity.QrRawPayload = metadata.QrRawPayload;
        entity.QrParsedJson = metadata.QrParsedJson;
        entity.SyncStatus = metadata.SyncStatus;

        entity.StorageObjectKey = objectKey;
        entity.StorageMetadataKey = metaKey;
        entity.OriginalFileName = safeFileName;
        entity.FileContentType = form.File?.ContentType ?? "application/octet-stream";
        entity.FileSize = form.File?.Length ?? 0;

        entity.MetadataJson = metadata.RawMetadataJson;
    }

    private static bool TryParseMetadata(string metadataJson, out ParsedMetadata metadata, out string? error)
    {
        metadata = new ParsedMetadata();
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;

            var documentId = GetGuid(root, "Id") ?? Guid.NewGuid();
            var createdAt = GetDateTime(root, "CreatedAt") ?? DateTime.UtcNow;

            metadata = new ParsedMetadata
            {
                DocumentId = documentId,
                CreatedAtUtc = NormalizeToUtc(createdAt),
                Source = GetString(root, "Source") ?? "Upload",
                DocumentType = GetString(root, "DocumentType") ?? string.Empty,
                Series = GetString(root, "Series") ?? string.Empty,
                DocumentNumber = GetString(root, "DocumentNumber") ?? string.Empty,
                DocumentDate = NormalizeToUtc(GetDateTime(root, "DocumentDate")),
                IssuerTaxId = GetString(root, "IssuerTaxId") ?? string.Empty,
                IssuerName = GetString(root, "IssuerName") ?? string.Empty,
                Atcud = GetString(root, "ATCUD") ?? string.Empty,
                QrRawPayload = GetString(root, "QrRawPayload") ?? string.Empty,
                QrParsedJson = GetRawJson(root, "QrParsedJson") ?? string.Empty,
                SyncStatus = GetString(root, "SyncStatus") ?? "PendingSync",
                RawMetadataJson = metadataJson
            };

            return true;
        }
        catch (JsonException ex)
        {
            error = $"metadata inválido: {ex.Message}";
            return false;
        }
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
        => value is null ? null : NormalizeToUtc(value.Value);
    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.TryGetProperty(propertyName, out value))
            return true;

        var camel = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        return root.TryGetProperty(camel, out value);
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static Guid? GetGuid(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var guid))
            return guid;

        return null;
    }

    private static DateTime? GetDateTime(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value))
            return null;

        if (value.ValueKind != JsonValueKind.String)
            return null;

        if (DateTime.TryParse(value.GetString(), out var dt))
            return dt;

        return null;
    }

    private static string? GetRawJson(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString();

        return value.GetRawText();
    }

    private sealed class ParsedMetadata
    {
        public Guid DocumentId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Source { get; set; } = "Upload";
        public string DocumentType { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime? DocumentDate { get; set; }
        public string IssuerTaxId { get; set; } = string.Empty;
        public string IssuerName { get; set; } = string.Empty;
        public string Atcud { get; set; } = string.Empty;
        public string QrRawPayload { get; set; } = string.Empty;
        public string QrParsedJson { get; set; } = string.Empty;
        public string SyncStatus { get; set; } = "PendingSync";
        public string RawMetadataJson { get; set; } = string.Empty;
    }

    public sealed class DocumentUploadForm
    {
        [FromForm(Name = "metadata")]
        public string Metadata { get; set; } = string.Empty;

        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }
    }

    public sealed class DocumentsQuery
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Nif { get; set; }
        public string? SeriesNumber { get; set; }
        public string? Atcud { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed class DocumentListItemResponse
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Source { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime? DocumentDate { get; set; }
        public string IssuerTaxId { get; set; } = string.Empty;
        public string IssuerName { get; set; } = string.Empty;
        public string Atcud { get; set; } = string.Empty;
        public string SyncStatus { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string StorageObjectKey { get; set; } = string.Empty;
    }

    public sealed class DocumentsPageResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<DocumentListItemResponse> Items { get; set; } = new();
    }
}


