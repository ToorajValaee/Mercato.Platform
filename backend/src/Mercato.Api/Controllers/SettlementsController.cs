using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/settlements")]
public class SettlementsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(Array.Empty<object>());
    }
}
