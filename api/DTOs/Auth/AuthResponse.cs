using Api.Models;

namespace Api.DTOs.Auth;

// Body untuk POST /api/auth/refresh dan POST /api/auth/logout.
public sealed record RefreshRequest(string? RefreshToken);

// Data user yang selamat dihantar kepada Flutter.
// PasswordHash sengaja tidak dimasukkan.
public sealed record UserResponse(
    int Id,
    string Name,
    string Email,
    DateTime CreatedAt)
{
    public static UserResponse From(ApplicationUser user) =>
        new(user.Id, user.Name, user.Email, user.CreatedAt);
}

// Response selepas register, login atau refresh.
public sealed record AuthResponse(
    string Token,
    string RefreshToken,
    UserResponse User);
