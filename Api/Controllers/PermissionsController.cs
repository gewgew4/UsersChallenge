using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController(IMediator mediator, ILogger<PermissionsController> logger) : ControllerBase
{
    public IActionResult Index()
    {
        return Ok();
    }
}
