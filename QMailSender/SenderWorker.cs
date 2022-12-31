using MediatR;
using Microsoft.EntityFrameworkCore;
using QMailSender.Entities;
using QMailSender.Handlers.Commands;
using QMailSender.Services.QueueService;

namespace QMailSender;

public class SenderWorker : BackgroundService
{
    private readonly DataContext _context;
    private readonly ILogger<SenderWorker> _logger;
    private readonly IMediator _mediator;
    private readonly IBackgroundTaskQueue _taskQueue;

    public SenderWorker(IBackgroundTaskQueue taskQueue, ILogger<SenderWorker> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        var scope = serviceScopeFactory.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<DataContext>();
        _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"{nameof(SenderWorker)} is running.");

        var waitingJobMembers = await _context.JobMembers.Include(i => i.Job)
            .Where(w => w.Job.Status == JobStatus.Waiting && w.Status == JobStatus.Waiting).ToListAsync();

        waitingJobMembers.ForEach(f =>
        {
            _taskQueue.QueueBackgroundWorkItemAsync(ct => Jobs.SendWorkAsync(f, stoppingToken));
        });

        return ProcessTaskQueueAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var workItem =
                    await _taskQueue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"{nameof(SenderWorker)} is stopping.");

        await base.StopAsync(stoppingToken);
    }

    // private async ValueTask SendWorkAsync(JobMember jobMember, CancellationToken token)
    // {
    //     var job = await _context.Jobs.FindAsync(jobMember.JobId);
    //
    //     if (job == null)
    //     {
    //         try
    //         {
    //             jobMember.Status = JobStatus.Failed;
    //             _context.JobMembers.Update(jobMember);
    //             await _context.SaveChangesAsync(token);
    //             return;
    //         }
    //         catch (Exception e)
    //         {
    //             _logger.LogError(
    //                 "JobId : {JobId} JobMember Id :{Id} TargetAddress: {TargetAdddress} ErrorMEssage : {Message}",
    //                 jobMember.JobId, jobMember.Id, jobMember.TargetAddress, e.Message);
    //             return;
    //         }
    //     }
    //
    //     if (job.Status is JobStatus.Failed or JobStatus.Finished ||
    //         jobMember.Status is JobStatus.Failed or JobStatus.Finished)
    //         return;
    //
    //     if (job.Status is JobStatus.Waiting or JobStatus.Queued)
    //     {
    //         try
    //         {
    //             job.Status = JobStatus.Running;
    //             _context.Jobs.Update(job);
    //             await _context.SaveChangesAsync(token);
    //         }
    //         catch (Exception e)
    //         {
    //             _logger.LogError(
    //                 "JobId : {JobId} JobMember Id :{Id} TargetAddress: {TargetAdddress} ErrorMEssage : {Message}",
    //                 jobMember.JobId, jobMember.Id, jobMember.TargetAddress, e.Message);
    //             return;
    //         }
    //     }
    //
    //     if (jobMember.Status is JobStatus.Waiting or JobStatus.Queued)
    //     {
    //         try
    //         {
    //             jobMember.Status = JobStatus.Running;
    //             jobMember.RunningTime = DateTime.Now;
    //             _context.JobMembers.Update(jobMember);
    //             await _context.SaveChangesAsync(token);
    //         }
    //         catch (Exception e)
    //         {
    //             _logger.LogError(
    //                 "JobId : {JobId} JobMember Id :{Id} TargetAddress: {TargetAdddress} ErrorMEssage : {Message}",
    //                 jobMember.JobId, jobMember.Id, jobMember.TargetAddress, e.Message);
    //             return;
    //         }
    //     }
    //
    //     var sendEmailCommand = new SendEmailCommand()
    //     {
    //         Job = job,
    //         JobMember = jobMember
    //     };
    //
    //     var result = await _mediator.Send(sendEmailCommand, token);
    //
    //     if (result.Success)
    //     {
    //         
    //     }
    //     
    //     await Task.Delay(TimeSpan.FromMilliseconds(job.Request.SendDelay), token);
    //
    //
    //     // Simulate three 5-second tasks to complete
    //     // for each enqueued work item
    //
    //     int delayLoop = 0;
    //
    //     _logger.LogInformation("Queued work item {TargetAddress} is starting.", jobMember.TargetAddress);
    //
    //     while (!token.IsCancellationRequested && delayLoop < 3)
    //     {
    //         try
    //         {
    //             await Task.Delay(TimeSpan.FromSeconds(5), token);
    //         }
    //         catch (OperationCanceledException)
    //         {
    //             // Prevent throwing if the Delay is cancelled
    //         }
    //
    //         ++delayLoop;
    //
    //         _logger.LogInformation("Queued work item {TargetAddress} is running. {DelayLoop}/3",
    //             jobMember.TargetAddress, delayLoop);
    //     }
    //
    //     string format = delayLoop switch
    //     {
    //         3 => "Queued Background Task {Guid} is complete.",
    //         _ => "Queued Background Task {Guid} was cancelled."
    //     };
    //
    //     _logger.LogInformation(format, jobMember.TargetAddress);
    // }
}