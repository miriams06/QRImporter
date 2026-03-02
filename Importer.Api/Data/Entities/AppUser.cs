using System.ComponentModel.DataAnnotations;

namespace Importer.Api.Data.Entities;

/// <summary>
/// Utilizador da aplicação (tabela Users).
/// </summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash bcrypt da password. NUNCA guardar em plain text.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Perfil RBAC: Admin | Operador | Revisor
    /// </summary>
    [Required, MaxLength(20)]
    public string Role { get; set; } = "Operador";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navegação
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}