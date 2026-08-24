using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransfersController : ControllerBase
{
    [HttpPost]
    public IActionResult Create()
    {
        return Accepted();
    }
}
