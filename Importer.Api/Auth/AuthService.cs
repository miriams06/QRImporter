using Importer.Api.Data;
using Importer.Api.Data.Entities;
using Importer.Core.Config;
using Microsoft.EntityFrameworkCore;

namespace Importer.Api.Auth;

/// <summary>
/// Lógica de negócio de autenticação.
/// - Login com bcrypt
/// - Emissão de access + refresh token
/// - Rotação de refresh token (1 uso por token)
/// - Logout (revogação)
/// </summary>
public sealed class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly ApiConfig _cfg;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, JwtService jwt, ApiConfig cfg, ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt;
        _cfg = cfg;
        _logger = logger;
    }

    /// <summary>
    /// Autentica o utilizador e devolve access + refresh tokens.
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(string username, string password, string? ipAddress)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Tentativa de login falhada para utilizador: {Username}", username);
            return null;
        }

        var accessToken = _jwt.GenerateAccessToken(user);
        var (refreshPlain, refreshHash) = _jwt.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_cfg.JwtRefreshTokenDays),
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshToken);

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Login bem-sucedido: {Username} ({Role})", user.Username, user.Role);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshPlain,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(_cfg.JwtAccessTokenMinutes),
            Username: user.Username,
            Role: user.Role
        );
    }

    /// <summary>
    /// Troca um refresh token por um novo par access + refresh (rotação).
    /// O token antigo é revogado imediatamente — cada token só pode ser usado 1 vez.
    /// </summary>
    public async Task<AuthResponse?> RefreshAsync(string refreshTokenPlain, string? ipAddress)
    {
        var hash = JwtService.HashRefreshToken(refreshTokenPlain);

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null || !stored.IsActive || !stored.User.IsActive)
        {
            _logger.LogWarning("Tentativa de refresh com token inválido/expirado/revogado.");
            return null;
        }

        // Revogar o token atual (rotação)
        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;

        // Emitir novos tokens
        var newAccessToken = _jwt.GenerateAccessToken(stored.User);
        var (newRefreshPlain, newRefreshHash) = _jwt.GenerateRefreshToken();

        var newRefresh = new RefreshToken
        {
            TokenHash = newRefreshHash,
            UserId = stored.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(_cfg.JwtRefreshTokenDays),
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(newRefresh);
        await _db.SaveChangesAsync();

        return new AuthResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshPlain,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(_cfg.JwtAccessTokenMinutes),
            Username: stored.User.Username,
            Role: stored.User.Role
        );
    }

    /// <summary>
    /// Revoga o refresh token (logout).
    /// </summary>
    public async Task<bool> LogoutAsync(string refreshTokenPlain)
    {
        var hash = JwtService.HashRefreshToken(refreshTokenPlain);

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && !t.IsRevoked);

        if (stored is null) return false;

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Cria um utilizador (usado no seed inicial / endpoint de admin).
    /// </summary>
    public async Task<AppUser> CreateUserAsync(string username, string password, string role)
    {
        var user = new AppUser
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }
}