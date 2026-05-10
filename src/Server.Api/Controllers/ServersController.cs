using Mediator;
using Microsoft.AspNetCore.Mvc;
using ssmsmcp.Application.ServerMeta;

namespace ssmsmcp.Server.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class ServerController(IMediator mediator)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetServerRequest(), cancellationToken);

        return Ok(result);
    }
}
