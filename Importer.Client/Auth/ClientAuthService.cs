using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Importer.Core.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace Importer.Client.Auth;

/// <summary>
/// Serviço de autenticação do client Blazor WASM.
///
/// DECISÃO DE DESIGN — tokens em memória:
/// - O access token fica apenas em memória (variável C#), nunca em localStorage.
/// - O refresh token fica num cookie HttpOnly gerido pelo servidor.
/// - Ao fechar o browser, a sessão termina (access token perde-se).
/// - O cookie HttpOnly persiste e permite renovar a sessão ao reabrir.
///
/// Registado como Singleton porque AuthenticationStateProvider tem de ser Singleton
/// no Blazor WASM. Por isso injeta IHttpClientFactory (também Singleton) em vez de
/// HttpClient (Scoped) — injectar Scoped num Singleton causa erro de arranque.
/// </summary>
public sealed class ClientAuthService : AuthenticationStateProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ClientAuthService> _logger;

    private string? _accessToken;
    private DateTime _accessTokenExpiry;
    private UserProfile? _currentUser;

    private static readonly TimeSpan RenewMargin = TimeSpan.FromMinutes(2);

    public ClientAuthService(IHttpClientFactory httpFactory, ILogger<ClientAuthService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    // Cria um cliente fresco por pedido — IHttpClientFactory é thread-safe e Singleton
    private HttpClient Http => _httpFactory.CreateClient("ApiClient");

    public bool IsAuthenticated => _currentUser is not null;
    public UserProfile? CurrentUser => _currentUser;

    // --- AuthenticationStateProvider ---

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser is null || string.IsNullOrEmpty(_accessToken))
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUser.UserId),
            new Claim(ClaimTypes.Name, _currentUser.Username),
            new Claim(ClaimTypes.Role, _currentUser.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    // --- Login ---

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var response = await Http.PostAsJsonAsync("api/auth/login", new { username, password });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login falhado: {Status}", response.StatusCode);
                return false;
            }

            var data = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (data is null) return false;

            ApplyTokens(data.AccessToken, data.AccessTokenExpiry, data.Username, data.Role);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login");
            return false;
        }
    }

    // --- Logout ---

    public async Task LogoutAsync()
    {
        try
        {
            await Http.PostAsync("api/auth/logout", null);
        }
        catch { /* ignora erros de rede no logout */ }
        finally
        {
            ClearSession();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    // --- Refresh automático ---

    public async Task<string?> GetValidAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return null;

        if (DateTime.UtcNow.Add(RenewMargin) >= _accessTokenExpiry)
            await TryRefreshAsync();

        return _accessToken;
    }

    public async Task<bool> TryRefreshAsync()
    {
        try
        {
            var response = await Http.PostAsync("api/auth/refresh", null);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Refresh token expirado ou inválido — sessão terminada.");
                ClearSession();
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return false;
            }

            var data = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (data is null) { ClearSession(); return false; }

            ApplyTokens(data.AccessToken, data.AccessTokenExpiry, data.Username, data.Role);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no refresh");
            return false;
        }
    }

    // --- Helpers ---

    private void ApplyTokens(string accessToken, DateTime expiry, string username, string role)
    {
        _accessToken = accessToken;
        _accessTokenExpiry = expiry;
        _currentUser = new UserProfile
        {
            UserId = ParseUserIdFromToken(accessToken) ?? "",
            Username = username,
            Role = Enum.TryParse<UserRole>(role, out var r) ? r : UserRole.Operador
        };
    }

    private void ClearSession()
    {
        _accessToken = null;
        _accessTokenExpiry = default;
        _currentUser = null;
    }

    private static string? ParseUserIdFromToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
        }
        catch { return null; }
    }

    private record LoginResponseDto(
        string AccessToken,
        DateTime AccessTokenExpiry,
        string Username,
        string Role
    );
}