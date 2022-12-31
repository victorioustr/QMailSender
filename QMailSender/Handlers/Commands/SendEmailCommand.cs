using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MimeKit.Encodings;
using MimeKit.Text;
using QMailSender.Entities;
using QMailSender.Handlers.Concrete;
using IResult = QMailSender.Handlers.Abstract.IResult;

namespace QMailSender.Handlers.Commands;

public class SendEmailCommand : IRequest<IResult>
{
    public JobMember JobMember { get; set; }

    public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, IResult>
    {
        private readonly DataContext _context;
        private readonly ILogger<SendEmailCommand> _logger;

        public SendEmailCommandHandler(IServiceScopeFactory serviceScopeFactory, ILogger<SendEmailCommand> logger)
        {
            var scope = serviceScopeFactory.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<DataContext>();
            _logger = logger;
        }

        public async Task<IResult> Handle(SendEmailCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Job Id : {JobId} Job Member Id : {Id} TargetAddress : {TargetAddress} Job process started",
                request.JobMember.JobId, request.JobMember.Id, request.JobMember.TargetAddress);

            var jobMember = _context.JobMembers
                .Include(i => i.Job)
                .FirstOrDefault(w => w.Id == request.JobMember.Id);

            if (jobMember == null)
            {
                return new Result(false, $"JobMember Id : {request.JobMember.Id} job member not found");
            }

            var job = jobMember.Job;

            if (job.Status is JobStatus.Failed or JobStatus.Finished ||
                jobMember.Status is JobStatus.Failed or JobStatus.Finished)
                return new Result(false, $"Job Id : {jobMember.JobId} job already finished or failed");

            if (job.Status is JobStatus.Waiting or JobStatus.Queued)
            {
                try
                {
                    job.Status = JobStatus.Running;
                    _context.Jobs.Update(job);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "JobId : {JobId} JobMember Id :{Id} TargetAddress: {TargetAdddress} ErrorMEssage : {Message}",
                        jobMember.JobId, jobMember.Id, jobMember.TargetAddress, e.Message);
                    return new Result(false, $"Job Id : {jobMember.JobId} job not found");
                }
            }

            if (jobMember.Status is JobStatus.Waiting or JobStatus.Queued)
            {
                try
                {
                    jobMember.Status = JobStatus.Running;
                    jobMember.RunningTime = DateTime.Now;
                    _context.JobMembers.Update(jobMember);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "JobId : {JobId} JobMember Id :{Id} TargetAddress: {TargetAdddress} ErrorMEssage : {Message}",
                        jobMember.JobId, jobMember.Id, jobMember.TargetAddress, e.Message);
                    return new Result(false, $"JobMember Id : {jobMember.Id} job member not found");
                }
            }

            try
            {
                var email = new MimeMessage()
                {
                    Subject = job.Request.Subject,
                    Body = new TextPart(TextFormat.Html) { Text = Encoding.UTF8.GetString(Convert.FromBase64String(job.Request.Base64Body)) },
                };
                email.From.Add(new MailboxAddress(job.Request.FromName, job.Request.FromAddress));
                email.ReplyTo.Add(new MailboxAddress(job.Request.ReplyToName, job.Request.ReplyToAddress));
                email.To.Add(MailboxAddress.Parse(jobMember.TargetAddress));
                
                if (!string.IsNullOrEmpty(job.Request.UnSubscribeUrl))
                    email.Headers.Add("List-Unsubscribe", job.Request.UnSubscribeUrl);

                var smtpSettings = job.Request.SmtpSettings;

                using var smtp = new SmtpClient();

                if (smtpSettings.UseSSL)
                    await smtp.ConnectAsync(smtpSettings.SmtpServer, smtpSettings.SmtpPort,
                        SecureSocketOptions.SslOnConnect, cancellationToken);
                else if (smtpSettings.UseStartTls)
                    await smtp.ConnectAsync(smtpSettings.SmtpServer, smtpSettings.SmtpPort,
                        SecureSocketOptions.StartTls, cancellationToken);
                else
                    await smtp.ConnectAsync(smtpSettings.SmtpServer, smtpSettings.SmtpPort,
                        SecureSocketOptions.Auto, cancellationToken);

                await smtp.AuthenticateAsync(smtpSettings.Username, smtpSettings.Password, cancellationToken);

                var result = await smtp.SendAsync(email, cancellationToken);
                _logger.LogInformation("Smtp Server Response : {Response}", result);
                await smtp.DisconnectAsync(true, cancellationToken);

                jobMember.Status = JobStatus.Finished;
                jobMember.FinishTime = DateTime.Now;
                _context.JobMembers.Update(jobMember);

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                jobMember.Status = JobStatus.Failed;
                jobMember.FinishTime = DateTime.Now;
                jobMember.Description = e.Message;
                _context.JobMembers.Update(jobMember);

                await _context.SaveChangesAsync(cancellationToken);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(job.Request.SendDelay), cancellationToken);

            _logger.LogInformation(
                "Job Id : {JobId} Job Member Id : {Id} TargetAddress : {TargetAddress} Job process finished",
                request.JobMember.JobId, request.JobMember.Id, request.JobMember.TargetAddress);

            return new DataResult<string>("", false);
        }
    }
}