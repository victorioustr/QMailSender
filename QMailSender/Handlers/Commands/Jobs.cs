using MediatR;
using QMailSender.Entities;

namespace QMailSender.Handlers.Commands;

public static class Jobs
{
    private static IMediator _mediator;
    
    public static void Configure(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public static async ValueTask SendWorkAsync(JobMember jobMember, CancellationToken token)
    {
        await _mediator.Send(new SendEmailCommand() { JobMember = jobMember });
    }
}