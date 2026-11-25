using Api.Data;
using Api.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

// Route asas ialah /api/users.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        // AsNoTracking sesuai untuk query baca sahaja.
        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAt)
            .Select(user => new UserResponse(
                user.Id,
                user.Name,
                user.Email,
                user.CreatedAt))
            .ToListAsync();

        // PasswordHash tidak dipilih dan tidak dihantar kepada Flutter.
        return Ok(users);
    }
}
