using EduMaxBot.Data;
using EduMaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace EduMaxBot.Services;

public class GroupService
{
    private readonly AppDbContext _db;

    public GroupService(AppDbContext db) { _db = db; }

    public async Task<Group> CreateGroupAsync(long ownerId, string name)
    {
        var g = new Group { OwnerId = ownerId, Name = name, CreatedAtUtc = DateTime.UtcNow };
        _db.Groups.Add(g);
        _db.GroupMembers.Add(new GroupMember { GroupId = g.Id, UserId = ownerId, Role = GroupRole.Teacher });
        await _db.SaveChangesAsync();
        return g;
    }

    public async Task<List<Group>> ListOwnedGroupsAsync(long ownerId)
    {
        return await _db.Groups
            .Where(g => g.OwnerId == ownerId)
            .OrderBy(g => g.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<string?> CreateInviteTokenAsync(long requesterId, Guid groupId)
    {
        var g = await _db.Groups.SingleOrDefaultAsync(x => x.Id == groupId);
        if (g is null || g.OwnerId != requesterId) return null;

        var token = new InviteToken { GroupId = groupId };
        _db.InviteTokens.Add(token);
        await _db.SaveChangesAsync();
        return token.Token;
    }

    public async Task<bool> JoinByTokenAsync(long userId, string token)
    {
        var inv = await _db.InviteTokens.SingleOrDefaultAsync(x => x.Token == token);
        if (inv is null) return false;

        var already = await _db.GroupMembers.AnyAsync(x => x.GroupId == inv.GroupId && x.UserId == userId);
        if (already) return false;

        _db.GroupMembers.Add(new GroupMember { GroupId = inv.GroupId, UserId = userId, Role = GroupRole.Student });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PromoteTeacherAsync(long ownerId, Guid groupId, long targetUserId)
    {
        var g = await _db.Groups.SingleOrDefaultAsync(x => x.Id == groupId);
        if (g is null || g.OwnerId != ownerId) return false;

        var member = await _db.GroupMembers.SingleOrDefaultAsync(x => x.GroupId == groupId && x.UserId == targetUserId);
        if (member is null)
        {
            _db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = targetUserId, Role = GroupRole.Teacher });
        }
        else
        {
            member.Role = GroupRole.Teacher;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<long>> GetStudentsAsync(Guid groupId)
    {
        return await _db.GroupMembers
            .Where(m => m.GroupId == groupId && m.Role == GroupRole.Student)
            .Select(m => m.UserId)
            .ToListAsync();
    }

    public async Task<List<long>> GetTeachersAsync(Guid groupId)
    {
        return await _db.GroupMembers
            .Where(m => m.GroupId == groupId && m.Role == GroupRole.Teacher)
            .Select(m => m.UserId)
            .ToListAsync();
    }

    public async Task<bool> IsTeacherAsync(Guid groupId, long userId)
    {
        return await _db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId && m.Role == GroupRole.Teacher);
    }

    public async Task<bool> IsMemberAsync(Guid groupId, long userId)
    {
        return await _db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
    }
}
