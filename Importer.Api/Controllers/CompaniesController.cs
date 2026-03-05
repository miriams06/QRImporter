using Importer.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Importer.Api.Controllers;

[ApiController]
[Route("companies")]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CompaniesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("lookup/{nif}")]
    public async Task<IActionResult> Lookup(string nif, CancellationToken ct)
    {
        var normalized = NormalizeNif(nif);
        if (string.IsNullOrWhiteSpace(normalized))
            return BadRequest("NIF inválido.");

        var company = await _db.Companies
            .AsNoTracking()
            .Where(c => c.TaxId == normalized && (!string.IsNullOrWhiteSpace(c.Name) || !string.IsNullOrWhiteSpace(c.Address)))
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => new
            {
                Nif = c.TaxId,
                Name = c.Name,
                Address = c.Address,
                Source = string.IsNullOrWhiteSpace(c.Source) ? "companies" : c.Source
            })
            .FirstOrDefaultAsync(ct);

        if (company is not null)
            return Ok(company);

        var match = await _db.Documents
            .AsNoTracking()
            .Where(d => d.IssuerTaxId == normalized && (!string.IsNullOrWhiteSpace(d.IssuerName) || !string.IsNullOrWhiteSpace(d.IssuerAddress)))
            .OrderByDescending(d => d.ReceivedAtUtc)
            .Select(d => new
            {
                Nif = d.IssuerTaxId,
                Name = d.IssuerName,
                Address = d.IssuerAddress,
                Source = "documents-history"
            })
            .FirstOrDefaultAsync(ct);

        return match is null ? NotFound() : Ok(match);
    }

    private static string NormalizeNif(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsDigit).ToArray());
    }
}
