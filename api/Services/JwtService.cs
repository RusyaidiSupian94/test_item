using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Configurations;
using Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

// JwtService bertanggungjawab mencipta access JWT sahaja.
// Logik database dan password diletakkan dalam AuthService.
public sealed class JwtService
{
    private readonly JwtSettings _settings;

    public JwtService(JwtSettings settings)
    {
        _settings = settings;

        if (string.IsNullOrWhiteSpace(_settings.Key) ||
            Encoding.UTF8.GetByteCount(_settings.Key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key mesti mengandungi sekurang-kurangnya 32 bytes.");
        }
    }

    public string CreateAccessToken(ApplicationUser user)
    {
        // Claims ialah maklumat user yang dimasukkan ke dalam token.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),

            // JTI memberikan ID unik kepada setiap access JWT.
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
