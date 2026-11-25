using System.Security.Claims;
using Api.DTOs.Auth;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Semua route dalam controller ini bermula dengan /api/auth.
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    // AuthService disediakan oleh dependency injection.
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return ToActionResult(result);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return ToActionResult(result);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken);
        return ToActionResult(result);
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    // GET /api/auth/me
    // Endpoint ini memerlukan Authorization: Bearer <access-token>.
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        // Baca user ID yang JwtService masukkan sebagai claim.
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserAsync(userId);
        return user is null
            ? NotFound(new { message = "User tidak dijumpai." })
            : Ok(user);
    }

    // Tukar AuthResult daripada service kepada HTTP response.
    private IActionResult ToActionResult(AuthResult result)
    {
        return result.StatusCode switch
        {
            400 => BadRequest(new { message = result.Message }),
            401 => Unauthorized(new { message = result.Message }),
            409 => Conflict(new { message = result.Message }),
            201 => Created(
                $"/api/users/{result.Response!.User.Id}",
                result.Response),
            _ => Ok(result.Response)
        };
    }
}
