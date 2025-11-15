namespace EduMaxBot.Models;

public class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssignmentId { get; set; }
    public long UserId { get; set; }
    public int VariantNumber { get; set; }

    public string? TextAnswer { get; set; }
    public string? FileUrl { get; set; }

    public DateTime SubmittedAt { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    // блокировка на проверку
    public long? LockedByReviewerId { get; set; }
    public DateTime? LockedAtUtc { get; set; }

    // оценка
    public int? Score { get; set; }
    public string? Comment { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
