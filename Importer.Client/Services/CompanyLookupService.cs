using System.Net.Http.Json;

namespace Importer.Client.Services;

public sealed class CompanyLookupService
{
    private readonly ApiClient _api;
    private readonly HttpClient _hostHttp;
    private readonly ILogger<CompanyLookupService> _logger;

    private Dictionary<string, CompanyDirectoryEntry>? _localDirectory;

    public CompanyLookupService(ApiClient api, HttpClient hostHttp, ILogger<CompanyLookupService> logger)
    {
        _api = api;
        _hostHttp = hostHttp;
        _logger = logger;
    }

    public async Task<CompanyLookupDto?> LookupByNifAsync(string? nif, CancellationToken ct = default)
    {
        var normalized = NormalizeNif(nif);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        try
        {
            var fromApi = await _api.LookupCompanyByNifAsync(normalized, ct);
            if (fromApi is not null)
                return fromApi;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Company lookup API falhou para NIF {Nif}", normalized);
        }

        var local = await GetLocalDirectoryAsync(ct);
        if (local.TryGetValue(normalized, out var entry))
        {
            return new CompanyLookupDto
            {
                Nif = normalized,
                Name = entry.Name,
                Address = entry.Address,
                Source = "local-directory"
            };
        }

        return null;
    }

    private async Task<Dictionary<string, CompanyDirectoryEntry>> GetLocalDirectoryAsync(CancellationToken ct)
    {
        if (_localDirectory is not null)
            return _localDirectory;

        try
        {
            var rows = await _hostHttp.GetFromJsonAsync<List<CompanyDirectoryEntry>>("sample-data/company-directory.json", ct)
                       ?? new List<CompanyDirectoryEntry>();

            _localDirectory = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Nif))
                .GroupBy(x => NormalizeNif(x.Nif))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToDictionary(g => g.Key, g => g.Last());
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Não foi possível carregar sample-data/company-directory.json");
            _localDirectory = new Dictionary<string, CompanyDirectoryEntry>(StringComparer.Ordinal);
        }

        return _localDirectory;
    }

    private static string NormalizeNif(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsDigit).ToArray());
    }

    private sealed class CompanyDirectoryEntry
    {
        public string Nif { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}