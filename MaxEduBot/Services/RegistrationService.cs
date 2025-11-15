using EduMaxBot.Data;
using EduMaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace EduMaxBot.Services;

public class RegistrationService
{
    private readonly AppDbContext _db;

    public RegistrationService(AppDbContext db) { _db = db; }

    public async Task<bool> TryRegisterAsync(long userId, string username, string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        var first = parts[0];
        var last = string.Join(' ', parts.Skip(1));

        var exists = await _db.Users.AnyAsync(x => x.UserId == userId);
        if (exists) return true;

        _db.Users.Add(new User
        {
            UserId = userId,
            FirstName = first,
            LastName = last,
            Username = username,
            RegisteredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }
}
