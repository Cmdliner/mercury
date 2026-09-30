using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercury.Api.Controllers;

[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Status = "Healthy" });
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult Private()
    {
        return Ok(new { Success = true});
    }
}