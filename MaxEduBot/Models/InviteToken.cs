namespace EduMaxBot.Models;

public class InviteToken
{
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public Guid GroupId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
