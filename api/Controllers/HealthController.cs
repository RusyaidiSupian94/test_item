using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Controller tambahan untuk menguji sama ada API masih hidup.
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    // GET /api/health
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "Api"
        });
    }
}
