using System.ComponentModel.DataAnnotations;

namespace Importer.Api.Data.Entities;

/// <summary>
/// Refresh token persistido em base de dados (tabela RefreshTokens).
/// Permite revogar sessões específicas e implementar rotação de tokens.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// O token em si (hash SHA-256 do valor enviado ao client).
    /// Guardar apenas o hash — se a DB vazar, os tokens não são utilizáveis.
    /// </summary>
    [Required]
    public string TokenHash { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP de origem da criação do token (para auditoria).
    /// </summary>
    [MaxLength(45)] // IPv6 max
    public string? CreatedByIp { get; set; }

    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}