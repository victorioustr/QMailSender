using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMailSender.Authorization;
using QMailSender.Entities;
using QMailSender.Handlers.Abstract;
using QMailSender.Handlers.Queries;
using QMailSender.Models;

namespace QMailSender.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    public class JobController : ControllerBase
    {
        private readonly IMediator _mediator;

        public JobController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<SendResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(Guid JobId)
        {
            var result = await _mediator.Send(new GetJobStatusQuery() { Id = JobId });
            return Ok(result);
        }
    }
}
