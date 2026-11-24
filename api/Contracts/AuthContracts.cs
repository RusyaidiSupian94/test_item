using AuthApi.Models;

namespace AuthApi.Contracts;

public sealed record RegisterRequest(string? Name, string? Email, string? Password);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record UserResponse(int Id, string Name, string Email, DateTime CreatedAt)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Name, user.Email, user.CreatedAt);
}

public sealed record AuthResponse(string Token, UserResponse User);
