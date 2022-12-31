namespace QMailSender.Entities;

public class JobMember
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string TargetAddress { get; set; }
    public DateTime QueueTime { get; set; }
    public DateTime? RunningTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public string? Description { get; set; }
    public JobStatus Status { get; set; }

    public virtual Job Job { get; set; }
}