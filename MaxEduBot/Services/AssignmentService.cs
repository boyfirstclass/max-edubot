using EduMaxBot.Data;
using EduMaxBot.Integrations;
using EduMaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace EduMaxBot.Services;

public class AssignmentService
{
    private readonly AppDbContext _db;
    private readonly GroupService _groups;
    private readonly MaxApiClient _max;

    public AssignmentService(AppDbContext db, GroupService groups, MaxApiClient max)
    {
        _db = db; _groups = groups; _max = max;
    }

    public record OpResult(bool Ok, string? Error = null, Guid? AssignmentId = null);

    public async Task<OpResult> CreateAssignmentAsync(long creatorId, Guid groupId, int variants, DateTime deadlineUtc, string title, string? description)
    {
        if (variants < 1 || variants > 100) return new(false, "VariantsCount должен быть 1..100");
        var g = await _db.Groups.SingleOrDefaultAsync(x => x.Id == groupId);
        if (g is null) return new(false, "Группа не найдена");

        var isTeacher = await _groups.IsTeacherAsync(groupId, creatorId);
        if (!isTeacher) return new(false, "Только преподаватель может создавать задания");

        var a = new Assignment
        {
            GroupId = groupId,
            CreatedBy = creatorId,
            VariantsCount = variants,
            DeadlineUtc = deadlineUtc,
            Title = title,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Assignments.Add(a);
        await _db.SaveChangesAsync();

        var students = await _groups.GetStudentsAsync(groupId);
        if (students.Count > 0)
        {
            int i = 0;
            foreach (var s in students.OrderBy(x => x))
            {
                var v = (i % variants) + 1;
                _db.AssignmentVariants.Add(new AssignmentVariant { AssignmentId = a.Id, UserId = s, VariantNumber = v });
                i++;
            }
            await _db.SaveChangesAsync();

            foreach (var s in students)
            {
                var v = await _db.AssignmentVariants.SingleAsync(x => x.AssignmentId == a.Id && x.UserId == s);
                await _max.SendTextAsync(s,
                    $"Новое задание ✅\n" +
                    $"AssignmentId: `{a.Id}`\n" +
                    $"Группа: `{a.GroupId}`\n" +
                    $"Название: {a.Title}\n" +
                    (string.IsNullOrWhiteSpace(a.Description) ? "" : $"Описание: {a.Description}\n") +
                    $"Ваш вариант: {v.VariantNumber}\n" +
                    $"Дедлайн (UTC): {a.DeadlineUtc:yyyy-MM-dd HH:mm}\n\n" +
                    $"Сдать: `/submit {a.Id} | <текст или ссылка на файл>`");
            }
        }

        var teachers = await _groups.GetTeachersAsync(groupId);
        foreach (var t in teachers)
        {
            await _max.SendTextAsync(t,
                $"Создано задание в группе `{g.Name}`\n" +
                $"AssignmentId: `{a.Id}`\nВариантов: {variants}\nДедлайн (UTC): {deadlineUtc:yyyy-MM-dd HH:mm}");
        }

        return new(true, AssignmentId: a.Id);
    }

    public async Task<OpResult> SubmitAsync(long userId, Guid assignmentId, string? answerText, string? fileUrl)
    {
        var a = await _db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId);
        if (a is null) return new(false, "Задание не найдено");

        if (!await _groups.IsMemberAsync(a.GroupId, userId))
            return new(false, "Вы не состоите в группе этого задания");

        if (DateTime.UtcNow > a.DeadlineUtc)
            return new(false, "Дедлайн уже прошёл");

        var av = await _db.AssignmentVariants.SingleOrDefaultAsync(x => x.AssignmentId == assignmentId && x.UserId == userId);
        if (av is null)
        {
            var all = await _db.GroupMembers.Where(m => m.GroupId == a.GroupId && m.Role == GroupRole.Student)
                .OrderBy(m => m.UserId).Select(m => m.UserId).ToListAsync();
            var idx = all.FindIndex(x => x == userId);
            var v = (idx >= 0 ? (idx % a.VariantsCount) : (int)(Math.Abs((int)userId) % a.VariantsCount)) + 1;
            av = new AssignmentVariant { AssignmentId = assignmentId, UserId = userId, VariantNumber = v };
            _db.AssignmentVariants.Add(av);
            await _db.SaveChangesAsync();
        }

        _db.Submissions.Add(new Submission
        {
            AssignmentId = assignmentId,
            UserId = userId,
            VariantNumber = av.VariantNumber,
            TextAnswer = answerText,
            FileUrl = fileUrl,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Pending
        });

        await _db.SaveChangesAsync();
        return new(true);
    }
}
