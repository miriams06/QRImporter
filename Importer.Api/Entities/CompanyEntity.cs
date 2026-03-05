namespace Importer.Api.Entities;

public class CompanyEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TaxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Source { get; set; } = "documents-sync";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
