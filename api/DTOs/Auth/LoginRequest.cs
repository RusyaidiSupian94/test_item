namespace Api.DTOs.Auth;

// Body untuk POST /api/auth/login.
public sealed record LoginRequest(string? Email, string? Password);
