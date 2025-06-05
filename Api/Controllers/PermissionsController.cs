using Application.Commands;
using Application.Dtos;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController(IMediator mediator, ILogger<PermissionsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> RequestPermission([FromBody] RequestPermissionCommand command)
    {
        logger.LogInformation("RequestPermission operation started");
        var result = await mediator.Send(command);
        logger.LogInformation("RequestPermission operation completed with ID: {PermissionId}", result);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> ModifyPermission(int id, [FromBody] ModifyPermissionCommand command)
    {
        logger.LogInformation("ModifyPermission operation started for ID: {PermissionId}", id);
        command.Id = id;
        await mediator.Send(command);
        logger.LogInformation("ModifyPermission operation completed for ID: {PermissionId}", id);

        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions()
    {
        logger.LogInformation("GetPermissions operation started");
        var result = await mediator.Send(new GetPermissionsQuery());
        logger.LogInformation("GetPermissions operation completed. Count: {Count}", result.Count());

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PermissionDto>> GetPermissionById(int id)
    {
        logger.LogInformation("GetPermissionById operation started for ID: {PermissionId}", id);
        var result = await mediator.Send(new GetPermissionByIdQuery(id));
        logger.LogInformation("GetPermissionById operation completed for ID: {PermissionId}", id);

        return Ok(result);
    }
}
