using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult Info()
    {
        return Ok(new
        {
            name = "Mercato Platform",
            version = "0.1-foundation"
        });
    }
}
