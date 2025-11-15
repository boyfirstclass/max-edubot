namespace EduMaxBot.Models;

public class ReviewSession
{
    public Guid AssignmentId { get; set; }
    public long ReviewerId { get; set; }
    public bool Active { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
