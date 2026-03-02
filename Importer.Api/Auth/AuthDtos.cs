using System.ComponentModel.DataAnnotations;

namespace Importer.Api.Auth;

// --- Request ---

public record LoginRequest(
    [Required, MaxLength(100)] string Username,
    [Required] string Password
);

public record RefreshRequest(
    [Required] string RefreshToken
);

// --- Response ---

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    string Username,
    string Role
);

public record TokenValidationResult(
    bool IsValid,
    string? UserId,
    string? Username,
    string? Role
);





