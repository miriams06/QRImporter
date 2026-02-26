using System.Net.Http.Headers;
using Importer.Core.Config;

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
    }
}
