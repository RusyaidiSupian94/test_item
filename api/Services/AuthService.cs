using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Api.Configurations;
using Api.Data;
using Api.DTOs.Auth;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

// AuthService menyimpan logic register, login, refresh token dan logout.
// Controller hanya menerima request dan pulangkan response.
public sealed class AuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher<ApplicationUser> passwordHasher,
        JwtService jwtService,
        JwtSettings jwtSettings)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var name = request.Name?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return AuthResult.BadRequest(
                "Nama wajib diisi dan maksimum 100 aksara.");
        }

        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 200 ||
            !MailAddress.TryCreate(email!, out _))
        {
            return AuthResult.BadRequest("Format email tidak sah.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return AuthResult.BadRequest(
                "Password mesti sekurang-kurangnya 6 aksara.");
        }

        if (await _db.Users.AnyAsync(user => user.Email == email))
        {
            return AuthResult.Conflict("Email ini sudah didaftarkan.");
        }

        var user = new ApplicationUser
        {
            Name = name!,
            Email = email!,
            CreatedAt = DateTime.UtcNow
        };

        // Password asal tidak disimpan.
        user.PasswordHash = _passwordHasher.HashPassword(user, password!);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var response = await CreateAuthResponseAsync(user);
        return AuthResult.Created(response);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.BadRequest("Email dan password wajib diisi.");
        }

        var user = await _db.Users
            .SingleOrDefaultAsync(item => item.Email == email);

        if (user is null)
        {
            return AuthResult.Unauthorized("Email atau password salah.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password!);

        if (verification == PasswordVerificationResult.Failed)
        {
            return AuthResult.Unauthorized("Email atau password salah.");
        }

        var response = await CreateAuthResponseAsync(user);
        return AuthResult.Success(response);
    }

    public async Task<AuthResult> RefreshAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return AuthResult.BadRequest("Refresh token wajib dihantar.");
        }

        // Cari menggunakan hash, bukan token asal.
        var tokenHash = HashToken(rawRefreshToken!);
        var storedToken = await _db.RefreshTokens
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash);

        if (storedToken is null ||
            storedToken.RevokedAt is not null ||
            storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return AuthResult.Unauthorized(
                "Refresh token tidak sah atau sudah tamat.");
        }

        // Rotation: token lama ditutup dan token baharu disimpan.
        storedToken.RevokedAt = DateTime.UtcNow;
        var newRawRefreshToken = CreateRawRefreshToken();
        var now = DateTime.UtcNow;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = HashToken(newRawRefreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenDays)
        });

        await _db.SaveChangesAsync();

        return AuthResult.Success(new AuthResponse(
            _jwtService.CreateAccessToken(storedToken.User),
            newRawRefreshToken,
            UserResponse.From(storedToken.User)));
    }

    public async Task LogoutAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var tokenHash = HashToken(rawRefreshToken!);
        var storedToken = await _db.RefreshTokens
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash);

        if (storedToken is not null && storedToken.RevokedAt is null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<UserResponse?> GetUserAsync(int userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId);

        return user is null ? null : UserResponse.From(user);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        ApplicationUser user)
    {
        var rawRefreshToken = CreateRawRefreshToken();
        var now = DateTime.UtcNow;

        // Simpan hash refresh token; token asal dipulangkan kepada Flutter.
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenDays)
        });

        await _db.SaveChangesAsync();

        return new AuthResponse(
            _jwtService.CreateAccessToken(user),
            rawRefreshToken,
            UserResponse.From(user));
    }

    private static string CreateRawRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

// Service result memudahkan controller menukar hasil logic kepada HTTP response.
public sealed record AuthResult(
    int StatusCode,
    string? Message,
    AuthResponse? Response)
{
    public static AuthResult BadRequest(string message) =>
        new(400, message, null);

    public static AuthResult Unauthorized(string message) =>
        new(401, message, null);

    public static AuthResult Conflict(string message) =>
        new(409, message, null);

    public static AuthResult Success(AuthResponse response) =>
        new(200, null, response);

    public static AuthResult Created(AuthResponse response) =>
        new(201, null, response);
}
