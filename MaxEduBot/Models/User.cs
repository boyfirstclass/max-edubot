namespace EduMaxBot.Models;

public class User
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Username { get; set; }
    public DateTime RegisteredAtUtc { get; set; }
}
