using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Importer.Client.Auth;

/// <summary>
/// DelegatingHandler que injeta o Bearer token em todos os pedidos ao HttpClient.
/// Tenta renovar automaticamente se o token estiver prestes a expirar.
/// 
/// Registado no Program.cs como handler do HttpClient nomeado "ApiClient".
/// </summary>
public sealed class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ClientAuthService _auth;

    public AuthorizationMessageHandler(ClientAuthService auth)
    {
        _auth = auth;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _auth.GetValidAccessTokenAsync();

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        // Se receber 401, o token expirou entretanto — tenta refresh e repete
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await _auth.TryRefreshAsync();
            if (refreshed)
            {
                token = await _auth.GetValidAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    // Clonar o pedido porque o HttpClient não permite reenviar o mesmo
                    var cloned = await CloneRequestAsync(request);
                    response = await base.SendAsync(cloned, cancellationToken);
                }
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}