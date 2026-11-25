using System.Text;
using Api.Configurations;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Baca connection string daripada appsettings.json.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? "Data Source=data/app.db";

var databaseDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    "data");
Directory.CreateDirectory(databaseDirectory);

// Daftarkan EF Core dan SQLite.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Daftarkan PasswordHasher untuk hash dan verify password.
builder.Services.AddSingleton<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

// Baca konfigurasi JWT secara manual supaya alirannya mudah difahami.
var expiresMinutes = int.TryParse(
    builder.Configuration["Jwt:ExpiresMinutes"],
    out var parsedExpiresMinutes)
    ? parsedExpiresMinutes
    : 15;

var refreshTokenDays = int.TryParse(
    builder.Configuration["Jwt:RefreshTokenDays"],
    out var parsedRefreshTokenDays)
    ? parsedRefreshTokenDays
    : 7;

var jwtSettings = new JwtSettings
{
    Key = builder.Configuration["Jwt:Key"] ?? string.Empty,
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "Api",
    Audience = builder.Configuration["Jwt:Audience"] ?? "FlutterApp",
    ExpiresMinutes = expiresMinutes,
    RefreshTokenDays = refreshTokenDays
};

if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key mesti mengandungi sekurang-kurangnya 32 bytes.");
}

// Daftarkan object config supaya JwtService dan AuthService boleh inject JwtSettings.
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddCors(options =>
{
    // Terbuka untuk Flutter Web semasa belajar.
    // Production perlu gunakan domain yang khusus.
    options.AddPolicy("Flutter", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Peraturan ini digunakan oleh attribute [Authorize].
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Register service yang digunakan oleh controller.
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// EnsureCreated sesuai untuk belajar kerana ia mencipta table secara automatik.
// Folder Migrations disediakan untuk digunakan apabila project dinaik taraf.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("Flutter");
app.UseAuthentication();
app.UseAuthorization();

// Aktifkan semua controller berdasarkan attribute route.
app.MapControllers();

app.Run();
