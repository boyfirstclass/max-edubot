namespace EduMaxBot.Models;

public class Assignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public long CreatedBy { get; set; }
    public int VariantsCount { get; set; }  // 1..100
    public DateTime DeadlineUtc { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
