using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Importer.Api.Data.Entities;
using Importer.Core.Config;
using Microsoft.IdentityModel.Tokens;

namespace Importer.Api.Auth;

/// <summary>
/// Geração e validação de JWT + refresh tokens.
/// Depende de ApiConfig (injetado via IOptions).
/// </summary>
public sealed class JwtService
{
    private readonly ApiConfig _cfg;

    public JwtService(ApiConfig cfg)
    {
        _cfg = cfg;
    }

    /// <summary>
    /// Gera um access token JWT assinado para o utilizador.
    /// </summary>
    public string GenerateAccessToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_cfg.JwtAccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _cfg.JwtIssuer,
            audience: _cfg.JwtAudience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Gera um refresh token aleatório seguro.
    /// Devolve o valor em plain (para enviar ao client) e o hash (para guardar na DB).
    /// </summary>
    public (string plain, string hash) GenerateRefreshToken()
    {
        var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashRefreshToken(plain);
        return (plain, hash);
    }

    /// <summary>
    /// Hash SHA-256 do refresh token.
    /// Guardamos apenas o hash na DB por segurança.
    /// </summary>
    public static string HashRefreshToken(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Valida um access token e devolve os claims.
    /// Usado em testes/admin — em pedidos normais o middleware faz a validação.
    /// </summary>
    public TokenValidationResult ValidateAccessToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.JwtSecret));
            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _cfg.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = _cfg.JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            return new TokenValidationResult(
                IsValid: true,
                UserId: principal.FindFirstValue(JwtRegisteredClaimNames.Sub),
                Username: principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName),
                Role: principal.FindFirstValue(ClaimTypes.Role)
            );
        }
        catch
        {
            return new TokenValidationResult(false, null, null, null);
        }
    }
}