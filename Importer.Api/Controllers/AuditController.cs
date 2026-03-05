using Importer.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Importer.Api.Controllers;

[ApiController]
[Route("audit")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("documents/{documentId:guid}")]
    public async Task<IActionResult> GetDocumentAudit(Guid documentId, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var safeTake = Math.Clamp(take, 1, 500);

        var items = await _db.AuditLogs
            .AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(safeTake)
            .Select(x => new
            {
                x.Id,
                x.CreatedAtUtc,
                x.DocumentId,
                x.EventType,
                x.Outcome,
                x.Actor,
                x.Source,
                x.DetailsJson
            })
            .ToListAsync(ct);

        return Ok(items);
    }
}