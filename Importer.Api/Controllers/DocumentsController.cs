using Importer.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Importer.Api.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController : ControllerBase
{
    private readonly IStorageService _storage;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IStorageService storage, ILogger<DocumentsController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Recebe:
    ///  - metadata (string JSON) no campo "metadata"
    ///  - ficheiro no campo "file"
    ///
    /// Guarda o ficheiro no MinIO (S3) e devolve a referência (objectKey).
    ///
    /// Próximos passos (quando entra Postgres):
    ///  - Validar JSON/Schema do metadata
    ///  - Persistir metadata + objectKey + estado de sync
    ///  - Gerar endpoints de listagem/histórico a partir da BD
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(100_000_000)] // 100MB (ajusta)
    public async Task<IActionResult> Upload([FromForm] DocumentUploadForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Metadata))
            return BadRequest("metadata é obrigatório.");

        if (form.File is null || form.File.Length == 0)
            return BadRequest("file é obrigatório.");

        // Segurança básica: evitar path traversal
        var safeFileName = Path.GetFileName(form.File.FileName);

        // id lógico do documento (mais tarde será PK na BD)
        var documentId = Guid.NewGuid();

        // objectKey dentro do bucket (estrutura previsível)
        // Ex: documents/9f.../fatura.jpg
        var objectKey = $"documents/{documentId:D}/{safeFileName}";
        var metaKey = $"documents/{documentId:D}/metadata.json";

        try
        {
            _logger.LogInformation(
                "Upload start: fileName={FileName} contentType={ContentType} size={Size} objectKey={ObjectKey} metaKey={MetaKey}",
                safeFileName,
                form.File.ContentType,
                form.File.Length,
                objectKey,
                metaKey
            );

            // Upload ficheiro para MinIO
            await using (var stream = form.File.OpenReadStream())
            {
                await _storage.PutAsync(
                    content: stream,
                    contentType: form.File.ContentType ?? "application/octet-stream",
                    objectKey: objectKey,
                    ct: ct
                );
            }

            // Upload metadata.json para MinIO
            var metaBytes = System.Text.Encoding.UTF8.GetBytes(form.Metadata);
            await using (var ms = new MemoryStream(metaBytes))
            {
                await _storage.PutAsync(
                    content: ms,
                    contentType: "application/json",
                    objectKey: metaKey,
                    ct: ct
                );
            }

            _logger.LogInformation(
                "Upload OK: documentId={DocumentId} objectKey={ObjectKey} metaKey={MetaKey}",
                documentId,
                objectKey,
                metaKey
            );

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
                "Upload cancelado (cliente desligou/cancelou): documentId={DocumentId} objectKey={ObjectKey}",
                documentId,
                objectKey
            );

            return Problem(
                title: "Upload cancelado",
                detail: "O upload foi cancelado.",
                statusCode: 499 // client closed request (não oficial, mas útil p/ debug)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Upload falhou: documentId={DocumentId} objectKey={ObjectKey} metaKey={MetaKey}",
                documentId,
                objectKey,
                metaKey
            );

            // Isto faz com que tu vejas a mensagem real no Network tab,
            // em vez de só “500 Internal Server Error”.
            return Problem(
                title: "Erro a guardar no storage (MinIO)",
                detail: ex.Message,
                statusCode: 502
            );
        }
    }

    public sealed class DocumentUploadForm
    {
        [FromForm(Name = "metadata")]
        public string Metadata { get; set; } = string.Empty;

        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }
    }
}