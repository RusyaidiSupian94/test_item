namespace Api.Configurations;

// Semua tetapan JWT dikumpulkan dalam satu object.
// Nilainya datang daripada section "Jwt" dalam appsettings.json.
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Api";

    public string Audience { get; set; } = "FlutterApp";

    // Tempoh access JWT dalam minit.
    public int ExpiresMinutes { get; set; } = 15;

    // Tempoh refresh token dalam hari.
    public int RefreshTokenDays { get; set; } = 7;
}
