using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

// DbContext ialah jambatan antara class C# dan table SQLite.
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}
