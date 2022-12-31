using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using QMailSender.Models;

namespace QMailSender.Entities;

public class Job
{
    public Job()
    {
        JobMembers = new HashSet<JobMember>();
    }

    public Guid Id { get; set; }

    [JsonIgnore] [Column("Request")] public string RequestString { get; set; }

    public JobStatus Status { get; set; }

    [NotMapped]
    public SendRequest Request
    {
        get => JsonSerializer.Deserialize<SendRequest>(RequestString);
        set => RequestString = JsonSerializer.Serialize(value);
    }

    public virtual IEnumerable<JobMember> JobMembers { get; set; }

    [NotMapped] public int? TotalMemberCount => JobMembers?.Count();

    [NotMapped] public int? QueuedMemberCount => JobMembers?.Count(c => c.Status == JobStatus.Queued);

    [NotMapped] public int? RunningMemberCount => JobMembers?.Count(c => c.Status == JobStatus.Running);

    [NotMapped] public int? FailedMemberCount => JobMembers?.Count(c => c.Status == JobStatus.Failed);

    [NotMapped] public int? WaitingMemberCount => JobMembers?.Count(c => c.Status == JobStatus.Waiting);

    [NotMapped] public int? FinishedMemberCount => JobMembers?.Count(c => c.Status == JobStatus.Finished);
}

public enum JobStatus
{
    Waiting,
    Queued,
    Running,
    Finished,
    Failed
}