namespace EduMaxBot.Models;

public class GroupMember
{
    public Guid GroupId { get; set; }
    public long UserId { get; set; }
    public GroupRole Role { get; set; } = GroupRole.Student;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
