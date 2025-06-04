using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class PermissionsController : ControllerBase
{
    public IActionResult Index()
    {
        return Ok();
    }
}
