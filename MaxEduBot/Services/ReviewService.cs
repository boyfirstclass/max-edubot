using System.Data;
using EduMaxBot.Data;
using EduMaxBot.Integrations;
using EduMaxBot.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EduMaxBot.Services;

public class ReviewService
{
    private readonly AppDbContext _db;
    private readonly GroupService _groups;
    private readonly MaxApiClient _max;

    public ReviewService(AppDbContext db, GroupService groups, MaxApiClient max)
    {
        _db = db; _groups = groups; _max = max;
    }

    public record OpResult(bool Ok, string? Error = null, Guid? AssignmentId = null);

    public async Task<OpResult> StartReviewAsync(long reviewerId, Guid assignmentId)
    {
        var a = await _db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId);
        if (a is null) return new(false, "Задание не найдено");
        if (!await _groups.IsTeacherAsync(a.GroupId, reviewerId)) return new(false, "Только преподаватель группы");

        var rs = await _db.ReviewSessions.SingleOrDefaultAsync(x => x.AssignmentId == assignmentId && x.ReviewerId == reviewerId);
        if (rs is null)
        {
            _db.ReviewSessions.Add(new ReviewSession
            {
                AssignmentId = assignmentId,
                ReviewerId = reviewerId,
                Active = true,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            rs.Active = true;
            rs.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return new(true);
    }

    public async Task StopReviewAsync(long reviewerId, Guid assignmentId)
    {
        var rs = await _db.ReviewSessions.SingleOrDefaultAsync(x => x.AssignmentId == assignmentId && x.ReviewerId == reviewerId);
        if (rs is null) return;
        rs.Active = false;
        rs.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsSessionActiveAsync(long reviewerId, Guid assignmentId)
    {
        var rs = await _db.ReviewSessions.SingleOrDefaultAsync(x => x.AssignmentId == assignmentId && x.ReviewerId == reviewerId);
        return rs?.Active == true;
    }

    public async Task SendNextForReviewAsync(long reviewerId, Guid assignmentId)
    {
        var claimedId = await ClaimNextSubmissionIdAsync(reviewerId, assignmentId);
        if (claimedId is null)
        {
            await _max.SendTextAsync(reviewerId, "Решений на проверку больше нет 🎉");
            return;
        }

        var sub = await _db.Submissions.SingleAsync(x => x.Id == claimedId);
        var u = await _db.Users.SingleAsync(x => x.UserId == sub.UserId);

        await _max.SendTextAsync(reviewerId,
            "Решение на проверку:\n" +
            $"SubmissionId: `{sub.Id}`\n" +
            $"От: {u.FirstName} {u.LastName} (UserId {u.UserId})\n" +
            $"Вариант: {sub.VariantNumber}\n" +
            $"Отправлено (UTC): {sub.SubmittedAt:yyyy-MM-dd HH:mm}\n" +
            (string.IsNullOrWhiteSpace(sub.FileUrl) ? "" : $"Файл/URL: {sub.FileUrl}\n") +
            (string.IsNullOrWhiteSpace(sub.TextAnswer) ? "" : $"Текст: {sub.TextAnswer}\n") +
            $"\nОцените командой:\n/grade {sub.Id} <0..100> | <комментарий>");
    }

    public async Task<OpResult> GradeAsync(long reviewerId, Guid submissionId, int score, string? comment)
    {
        if (score < 0 || score > 100) return new(false, "Оценка 0..100");

        var sub = await _db.Submissions.SingleOrDefaultAsync(x => x.Id == submissionId);
        if (sub is null) return new(false, "Submission не найден");
        var a = await _db.Assignments.SingleAsync(x => x.Id == sub.AssignmentId);

        if (!await _groups.IsTeacherAsync(a.GroupId, reviewerId))
            return new(false, "Только преподаватель группы");

        if (sub.Status == SubmissionStatus.Pending)
            return new(false, "Эта работа ещё не выдана на проверку (используйте /start_review)");

        if (sub.Status == SubmissionStatus.InReview && sub.LockedByReviewerId != reviewerId)
            return new(false, "Работа забронирована другим преподавателем");

        sub.Score = score;
        sub.Comment = comment;
        sub.ReviewedAtUtc = DateTime.UtcNow;
        sub.Status = SubmissionStatus.Reviewed;
        sub.LockedByReviewerId = reviewerId;

        await _db.SaveChangesAsync();

        await _max.SendTextAsync(sub.UserId,
            $"Ваша работа по `{a.Title}` проверена ✅\n" +
            $"Оценка: {score}\n" +
            (string.IsNullOrWhiteSpace(comment) ? "" : $"Комментарий: {comment}"));

        return new(true, AssignmentId: a.Id);
    }

    private async Task<Guid?> ClaimNextSubmissionIdAsync(long reviewerId, Guid assignmentId)
    {
        await using var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE ""Submissions"" s
   SET ""Status"" = 1,        -- InReview
       ""LockedByReviewerId"" = @rid,
       ""LockedAtUtc"" = (NOW() AT TIME ZONE 'UTC')
 WHERE s.""Id"" = (
     SELECT ""Id""
       FROM ""Submissions""
      WHERE ""AssignmentId"" = @aid
        AND ""Status"" = 0   -- Pending
      ORDER BY ""SubmittedAt""
      FOR UPDATE SKIP LOCKED
      LIMIT 1
 )
RETURNING s.""Id"";";

        var pRid = cmd.CreateParameter(); pRid.ParameterName = "@rid"; pRid.Value = reviewerId;
        var pAid = cmd.CreateParameter(); pAid.ParameterName = "@aid"; pAid.Value = assignmentId;
        cmd.Parameters.Add(pRid);
        cmd.Parameters.Add(pAid);

        var result = await cmd.ExecuteScalarAsync();
        return result is Guid g ? g : (Guid?)null;
    }
}
