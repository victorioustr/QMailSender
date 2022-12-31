using MediatR;
using Microsoft.AspNetCore.Mvc;
using QMailSender.Authorization;
using QMailSender.Handlers.Abstract;
using QMailSender.Handlers.Commands;
using QMailSender.Models;

namespace QMailSender.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SenderController : ControllerBase
{
    private readonly IMediator _mediator;

    public SenderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Produces("application/json", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<SendResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendRequest request)
    {
        var result = await _mediator.Send(new SendCommand { SendRequest = request });
        return Ok(result);
    }
}