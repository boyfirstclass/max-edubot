namespace EduMaxBot.Models;

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long OwnerId { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
