using Microsoft.AspNetCore.Mvc;

namespace Onboarding.WebApi.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public ActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}
