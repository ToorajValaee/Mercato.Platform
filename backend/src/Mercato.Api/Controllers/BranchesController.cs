using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/branches")]
public class BranchesController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(Array.Empty<object>());
    }
}
