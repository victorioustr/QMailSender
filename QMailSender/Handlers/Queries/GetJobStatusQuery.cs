using MediatR;
using Microsoft.Build.Logging;
using Microsoft.EntityFrameworkCore;
using QMailSender.Entities;
using QMailSender.Handlers.Abstract;
using QMailSender.Handlers.Concrete;

namespace QMailSender.Handlers.Queries;

public class GetJobStatusQuery : IRequest<IDataResult<Job>>
{
    public Guid Id { get; set; }

    public class GetShareQueryHandler : IRequestHandler<GetJobStatusQuery, IDataResult<Job>>
    {
        private readonly DataContext _context;

        public GetShareQueryHandler(DataContext context)
        {
            _context = context;
        }

        public async Task<IDataResult<Job>> Handle(GetJobStatusQuery request, CancellationToken cancellationToken)
        {
            var result = await _context.Jobs
                .Include(i => i.JobMembers)
                .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken: cancellationToken);

            return new DataResult<Job>(result ?? throw new BadHttpRequestException(nameof(Job)), true);
        }
    }
}