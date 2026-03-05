using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Importer.Client.Services
{
    /// <summary>
    /// Cliente base para chamadas à API.
    /// URLs são sempre resolvidas via AppConfig.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(HttpClient http)
            => _http = http;

        public async Task UploadDocumentAsync(string jsonPayload, string fileName, string contentType, byte[] fileBytes)
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(jsonPayload), "metadata");

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            var resp = await _http.PostAsync("documents", form);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<CompanyLookupDto?> LookupCompanyByNifAsync(string nif, CancellationToken ct = default)
        {
            var resp = await _http.GetAsync($"companies/lookup/{Uri.EscapeDataString(nif)}", ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<CompanyLookupDto>(cancellationToken: ct);
        }
    }

    public sealed class CompanyLookupDto
    {
        public string Nif { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
