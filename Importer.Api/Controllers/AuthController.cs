using Importer.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Importer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Devolve access token (no body) e refresh token (em cookie HttpOnly).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.LoginAsync(request.Username, request.Password, ip);

        if (result is null)
            return Unauthorized(new { message = "Credenciais inválidas." });

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new
        {
            accessToken = result.AccessToken,
            accessTokenExpiry = result.AccessTokenExpiry,
            username = result.Username,
            role = result.Role
        });
    }

    /// <summary>
    /// POST /api/auth/refresh
    /// Lê o refresh token do cookie HttpOnly e devolve novo par de tokens.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { message = "Refresh token em falta." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.RefreshAsync(refreshToken, ip);

        if (result is null)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new { message = "Refresh token inválido ou expirado." });
        }

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new
        {
            accessToken = result.AccessToken,
            accessTokenExpiry = result.AccessTokenExpiry,
            username = result.Username,
            role = result.Role
        });
    }

    /// <summary>
    /// POST /api/auth/logout
    /// Revoga o refresh token e limpa o cookie.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await _auth.LogoutAsync(refreshToken);

        ClearRefreshTokenCookie();
        return NoContent();
    }

    // --- Helpers ---

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,        // não acessível por JS
            Secure = true,          // apenas HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }
}