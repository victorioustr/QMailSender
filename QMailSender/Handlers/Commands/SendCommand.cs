using MediatR;
using QMailSender.Entities;
using QMailSender.Handlers.Abstract;
using QMailSender.Handlers.Concrete;
using QMailSender.Models;
using QMailSender.Services.QueueService;

namespace QMailSender.Handlers.Commands;

public class SendCommand : IRequest<IDataResult<SendResponse>>
{
    public SendRequest SendRequest { get; set; }

    public class SendCommandHandler : IRequestHandler<SendCommand, IDataResult<SendResponse>>
    {
        private readonly DataContext _context;
        private readonly IBackgroundTaskQueue _taskQueue;

        public SendCommandHandler(DataContext context, IBackgroundTaskQueue taskQueue)
        {
            _context = context;
            _taskQueue = taskQueue;
        }

        public async Task<IDataResult<SendResponse>> Handle(SendCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var sendRequest = request.SendRequest;

                var job = new Job
                {
                    Id = Guid.NewGuid(),
                    Request = sendRequest,
                    Status = JobStatus.Waiting
                };

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync(cancellationToken);

                var jobMembers = sendRequest.TargetAddresses.ToList().Select(s => new JobMember
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    Status = JobStatus.Waiting,
                    TargetAddress = s,
                    QueueTime = DateTime.Now
                }).ToList();

                await _context.JobMembers.AddRangeAsync(jobMembers, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                jobMembers.ForEach(f =>
                {
                    f.Status = JobStatus.Queued;
                    _context.JobMembers.Update(f);
                    _context.SaveChangesAsync(cancellationToken);
                    _taskQueue.QueueBackgroundWorkItemAsync(ct => Jobs.SendWorkAsync(f, cancellationToken));
                });

                var result = new SendResponse
                {
                    Count = jobMembers.Count,
                    JobId = job.Id
                };

                return new DataResult<SendResponse>(result, true);
            }
            catch (Exception e)
            {
                return new DataResult<SendResponse>(new SendResponse(), false, e.Message);
            }
        }
    }
}