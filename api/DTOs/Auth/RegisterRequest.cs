namespace Api.DTOs.Auth;

// Body untuk POST /api/auth/register.
public sealed record RegisterRequest(
    string? Name,
    string? Email,
    string? Password);
