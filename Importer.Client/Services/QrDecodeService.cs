using System.Diagnostics;
using Importer.Client.Models;
using Microsoft.JSInterop;

namespace Importer.Client.Services
{
    /// <summary>
    /// Serviço de leitura de QR Code ATCUD.
    /// Decode é feito 100% client-side via JS/Web Worker.
    /// </summary>
    public sealed class QrDecodeService
    {
        private readonly IJSRuntime _js;

        public QrDecodeService(IJSRuntime js) => _js = js;

        public async Task<QrResult> DecodeAsync(byte[] fileBytes, string contentType, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var result = new QrResult();

            if (fileBytes is null || fileBytes.Length == 0 || string.IsNullOrWhiteSpace(contentType))
            {
                result.Status = "UNREADABLE";
                result.Strategy = "invalid-input";
                result.DurationMs = sw.ElapsedMilliseconds;
                return result;
            }

            try
            {
                if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return await DecodeImageAsync(fileBytes, contentType, sw, cancellationToken);
                }

                if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return await DecodePdfAsync(fileBytes, sw, cancellationToken);
                }

                result.Status = "UNREADABLE";
                result.Strategy = "unsupported-content-type";
                result.DurationMs = sw.ElapsedMilliseconds;
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Status = "CANCELLED";
                result.Strategy = "cancelled";
                result.DurationMs = sw.ElapsedMilliseconds;
                return result;
            }
            catch
            {
                result.Status = "UNREADABLE";
                result.Strategy = "exception";
                result.DurationMs = sw.ElapsedMilliseconds;
                return result;
            }
        }

        private async Task<QrResult> DecodeImageAsync(byte[] fileBytes, string contentType, Stopwatch sw, CancellationToken ct)
        {
            var result = new QrResult { Strategy = "image-worker-pipeline" };
            var timeouts = new[] { 3500, 6000, 9000 };

            for (var i = 0; i < timeouts.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                result.Attempts++;

                var payload = await TryDecodeImageOnceAsync(fileBytes, contentType, timeouts[i], ct);
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    result.Payload = payload;
                    result.Status = "OK";
                    result.DurationMs = sw.ElapsedMilliseconds;
                    return result;
                }
            }

            result.Status = "NOT_FOUND";
            result.DurationMs = sw.ElapsedMilliseconds;
            return result;
        }

        private async Task<QrResult> DecodePdfAsync(byte[] fileBytes, Stopwatch sw, CancellationToken ct)
        {
            var result = new QrResult { Strategy = "pdf-render-decode" };
            var base64 = Convert.ToBase64String(fileBytes);

            var dpis = new[] { 160, 220, 300 };
            int pages = 1;

            foreach (var dpi in dpis)
            {
                for (var page = 1; page <= Math.Min(3, pages); page++)
                {
                    ct.ThrowIfCancellationRequested();
                    result.Attempts++;

                    var decoded = await _js.InvokeAsync<PdfDecodeResult>(
                        "pdfInterop.renderAndDecodeFromBase64",
                        ct,
                        base64,
                        "pdfCanvas",
                        page,
                        dpi);

                    if (decoded is not null)
                    {
                        pages = Math.Max(1, decoded.Pages);

                        if (!string.IsNullOrWhiteSpace(decoded.QrText))
                        {
                            result.Payload = decoded.QrText;
                            result.Status = "OK";
                            result.PageNumber = page;
                            result.Strategy = $"pdf-dpi-{dpi}";
                            result.DurationMs = sw.ElapsedMilliseconds;
                            return result;
                        }
                    }
                }
            }

            result.Status = "NOT_FOUND";
            result.DurationMs = sw.ElapsedMilliseconds;
            return result;
        }

        private async Task<string?> TryDecodeImageOnceAsync(byte[] fileBytes, string contentType, int timeoutMs, CancellationToken ct)
        {
            try
            {
                return await _js.InvokeAsync<string>(
                    "qrInterop.decodeQrFromBytes",
                    ct,
                    fileBytes,
                    contentType,
                    timeoutMs);
            }
            catch
            {
                return null;
            }
        }

        private sealed class PdfDecodeResult
        {
            public int Pages { get; set; }
            public string? QrText { get; set; }
        }
    }
}
