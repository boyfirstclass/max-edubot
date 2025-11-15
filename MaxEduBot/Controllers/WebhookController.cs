using EduMaxBot.Data;
using EduMaxBot.Integrations;
using EduMaxBot.Services;
using EduMaxBot.Transport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduMaxBot.Controllers;

[ApiController]
[Route("max/webhook")]
public class WebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MaxApiClient _max;
    private readonly IOptions<MaxApiOptions> _opt;
    private readonly RegistrationService _reg;
    private readonly GroupService _groups;
    private readonly AssignmentService _assignments;
    private readonly ReviewService _reviews;
    private readonly ILogger<WebhookController> _log;

    public WebhookController(
        AppDbContext db,
        MaxApiClient max,
        IOptions<MaxApiOptions> opt,
        RegistrationService reg,
        GroupService groups,
        AssignmentService assignments,
        ReviewService reviews,
        ILogger<WebhookController> log)
    {
        _db = db;
        _max = max;
        _opt = opt;
        _reg = reg;
        _groups = groups;
        _assignments = assignments;
        _reviews = reviews;
        _log = log;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] UpdateDto update)
    {
        // Проверка секрета
        var secret = Request.Headers["X-Max-Bot-Api-Secret"].ToString();
        if (!string.IsNullOrEmpty(_opt.Value.WebhookSecret) &&
            secret != _opt.Value.WebhookSecret)
        {
            _log.LogWarning("Webhook secret mismatch");
            return Unauthorized();
        }

        if (update.update_type != "message_created" || update.message?.body?.text is null)
            return Ok();

        var text = update.message.body.text.Trim();
        var userId = update.message.sender.user_id;
        var username = update.message.sender.username ?? "";
        _log.LogInformation("Incoming from {UserId}: {Text}", userId, text);

        var isReg = await _db.Users.AnyAsync(u => u.UserId == userId);

        // /start
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            if (isReg)
            {
                await _max.SendTextAsync(userId,
                    "Вы уже зарегистрированы ✅\n\n" +
                    "Команды:\n" +
                    "/help — список команд\n" +
                    "/create_group <Название>\n" +
                    "/my_groups\n" +
                    "/invite <GroupId>\n" +
                    "/join <Token>\n" +
                    "/promote_teacher <GroupId> <UserId>\n\n" +
                    "/new_assignment <GroupId> | <Variants 1-100> | <Deadline UTC yyyy-MM-dd HH:mm> | <Title> | <Description>\n" +
                    "/submit <AssignmentId> | <TextOrFileUrl>\n" +
                    "/start_review <AssignmentId>\n" +
                    "/stop_review <AssignmentId>\n" +
                    "/grade <SubmissionId> <Score 0..100> | <Comment>");
            }
            else
            {
                await _max.SendTextAsync(userId,
                    "Привет! Для начала напишите, пожалуйста, Имя и Фамилию одной строкой.\n" +
                    "Например: `Иван Петров`");
            }
            return Ok();
        }

        // /help
        if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            await _max.SendTextAsync(userId,
                "Команды:\n" +
                "/create_group <Название>\n" +
                "/my_groups\n" +
                "/invite <GroupId>\n" +
                "/join <Token>\n" +
                "/promote_teacher <GroupId> <UserId>\n\n" +
                "/new_assignment <GroupId> | <Variants 1-100> | <Deadline UTC yyyy-MM-dd HH:mm> | <Title> | <Description>\n" +
                "/submit <AssignmentId> | <TextOrFileUrl>\n" +
                "/start_review <AssignmentId>\n" +
                "/stop_review <AssignmentId>\n" +
                "/grade <SubmissionId> <Score 0..100> | <Comment>");
            return Ok();
        }

        // Регистрация: "Имя Фамилия" (если не зарегистрирован)
        if (!isReg && !text.StartsWith("/"))
        {
            var ok = await _reg.TryRegisterAsync(userId, username, text);
            if (ok)
            {
                await _max.SendTextAsync(userId,
                    "Вы зарегистрированы ✅\n" +
                    "Теперь можно:\n" +
                    "/create_group <Название>\n" +
                    "/help — список всех команд");
            }
            else
            {
                await _max.SendTextAsync(userId,
                    "Не смог распознать Имя и Фамилию. Напишите, например: `Иван Петров`");
            }
            return Ok();
        }

        // Остальные команды — только для зарегистрированных
        if (!isReg)
        {
            await _max.SendTextAsync(userId,
                "Сначала зарегистрируйтесь. Напишите Имя и Фамилию, например: `Иван Петров`");
            return Ok();
        }

        // --- Группы ---
        if (text.StartsWith("/create_group", StringComparison.OrdinalIgnoreCase))
        {
            var name = text.Substring("/create_group".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await _max.SendTextAsync(userId, "Использование: /create_group <Название>");
                return Ok();
            }

            var g = await _groups.CreateGroupAsync(userId, name);
            await _max.SendTextAsync(userId,
                $"Группа создана ✅\nID: `{g.Id}`\nНазвание: {g.Name}");
            return Ok();
        }

        if (text.Equals("/my_groups", StringComparison.OrdinalIgnoreCase))
        {
            var list = await _groups.ListOwnedGroupsAsync(userId);
            if (list.Count == 0)
            {
                await _max.SendTextAsync(userId, "У вас нет групп. Создайте: /create_group <Название>");
                return Ok();
            }
            var lines = string.Join("\n", list.Select(g => $"• {g.Name} (`{g.Id}`)"));
            await _max.SendTextAsync(userId, "Ваши группы:\n\n" + lines);
            return Ok();
        }

        if (text.StartsWith("/invite", StringComparison.OrdinalIgnoreCase))
        {
            var arg = text.Substring("/invite".Length).Trim();
            if (!Guid.TryParse(arg, out var groupId))
            {
                await _max.SendTextAsync(userId, "Использование: /invite <GroupId>");
                return Ok();
            }

            var token = await _groups.CreateInviteTokenAsync(userId, groupId);
            if (token is null)
            {
                await _max.SendTextAsync(userId, "Не удалось создать инвайт. Проверьте, что вы владелец группы.");
                return Ok();
            }

            await _max.SendTextAsync(userId,
                "Приглашение создано ✅\n" +
                $"Токен: `{token}`\n" +
                $"Вступить можно командой: `/join {token}`");
            return Ok();
        }

        if (text.StartsWith("/join", StringComparison.OrdinalIgnoreCase))
        {
            var token = text.Substring("/join".Length).Trim();
            var ok = await _groups.JoinByTokenAsync(userId, token);
            if (!ok)
            {
                await _max.SendTextAsync(userId, "Инвайт недействителен или вы уже в группе.");
                return Ok();
            }
            await _max.SendTextAsync(userId, "Вы вступили в группу как студент ✅");
            return Ok();
        }

        if (text.StartsWith("/promote_teacher", StringComparison.OrdinalIgnoreCase))
        {
            var args = text.Substring("/promote_teacher".Length).Trim();
            var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var groupId) || !long.TryParse(parts[1], out var targetUserId))
            {
                await _max.SendTextAsync(userId, "Использование: /promote_teacher <GroupId> <UserId>");
                return Ok();
            }

            var ok = await _groups.PromoteTeacherAsync(ownerId: userId, groupId: groupId, targetUserId: targetUserId);
            await _max.SendTextAsync(userId, ok ? "Пользователь назначен преподавателем ✅" : "Не получилось. Требуется быть владельцем группы.");
            return Ok();
        }

        // --- Задания ---
        if (text.StartsWith("/new_assignment", StringComparison.OrdinalIgnoreCase))
        {
            // формат: /new_assignment <GroupId> | <Variants 1..100> | <Deadline UTC yyyy-MM-dd HH:mm> | <Title> | <Description?>
            var payload = text.Substring("/new_assignment".Length).Trim();
            var parts = payload.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                await _max.SendTextAsync(userId,
                    "Использование:\n" +
                    "/new_assignment <GroupId> | <Variants 1-100> | <Deadline UTC yyyy-MM-dd HH:mm> | <Title> | <Description?>");
                return Ok();
            }

            if (!Guid.TryParse(parts[0], out var groupId) ||
                !int.TryParse(parts[1], out var variants) ||
                !DateTime.TryParse(parts[2], out var deadlineUtc))
            {
                await _max.SendTextAsync(userId, "Проверьте формат GroupId / Variants / Deadline.");
                return Ok();
            }

            var title = parts[3];
            var description = parts.Length >= 5 ? parts[4] : "";

            var result = await _assignments.CreateAssignmentAsync(userId, groupId, variants, DateTime.SpecifyKind(deadlineUtc, DateTimeKind.Utc), title, description);
            if (!result.Ok)
            {
                await _max.SendTextAsync(userId, result.Error ?? "Не удалось создать задание.");
                return Ok();
            }

            await _max.SendTextAsync(userId, $"Задание создано ✅\nAssignmentId: `{result.AssignmentId}`\nВариантов: {variants}\nДедлайн (UTC): {deadlineUtc:yyyy-MM-dd HH:mm}\n\nРассылаю студентам...");
            // Рассылка студентам — внутри сервиса уже сделана
            return Ok();
        }

        if (text.StartsWith("/submit", StringComparison.OrdinalIgnoreCase))
        {
            // формат: /submit <AssignmentId> | <TextOrUrl>
            var payload = text.Substring("/submit".Length).Trim();
            var parts = payload.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !Guid.TryParse(parts[0], out var assignmentId))
            {
                await _max.SendTextAsync(userId, "Использование: /submit <AssignmentId> | <Текст или URL>");
                return Ok();
            }

            var answer = parts[1];
            var res = await _assignments.SubmitAsync(userId, assignmentId, answerText: answer, fileUrl: ExtractUrl(answer));
            await _max.SendTextAsync(userId, res.Ok ? "Решение принято ✅" : ("Не удалось: " + res.Error));
            return Ok();
        }

        // --- Проверка ---
        if (text.StartsWith("/start_review", StringComparison.OrdinalIgnoreCase))
        {
            var arg = text.Substring("/start_review".Length).Trim();
            if (!Guid.TryParse(arg, out var assignmentId))
            {
                await _max.SendTextAsync(userId, "Использование: /start_review <AssignmentId>");
                return Ok();
            }

            var res = await _reviews.StartReviewAsync(userId, assignmentId);
            await _max.SendTextAsync(userId, res.Ok ? "Режим проверки запущен ✅" : ("Не удалось: " + res.Error));
            if (res.Ok)
            {
                // сразу отправим первую работу
                await _reviews.SendNextForReviewAsync(userId, assignmentId);
            }
            return Ok();
        }

        if (text.StartsWith("/stop_review", StringComparison.OrdinalIgnoreCase))
        {
            var arg = text.Substring("/stop_review".Length).Trim();
            if (!Guid.TryParse(arg, out var assignmentId))
            {
                await _max.SendTextAsync(userId, "Использование: /stop_review <AssignmentId>");
                return Ok();
            }
            await _reviews.StopReviewAsync(userId, assignmentId);
            await _max.SendTextAsync(userId, "Режим проверки остановлен ⏸️");
            return Ok();
        }

        if (text.StartsWith("/grade", StringComparison.OrdinalIgnoreCase))
        {
            // формат: /grade <SubmissionId> <Score> | <Comment>
            var payload = text.Substring("/grade".Length).Trim();
            var parts = payload.Split('|', 2, StringSplitOptions.TrimEntries); // [ "subId score", "comment" ]
            if (parts.Length == 0)
            {
                await _max.SendTextAsync(userId, "Использование: /grade <SubmissionId> <Score 0..100> | <Комментарий>");
                return Ok();
            }
            var main = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (main.Length < 2 || !Guid.TryParse(main[0], out var subId) || !int.TryParse(main[1], out var score))
            {
                await _max.SendTextAsync(userId, "Формат: /grade <SubmissionId> <Score 0..100> | <Комментарий>");
                return Ok();
            }
            var comment = parts.Length == 2 ? parts[1] : "";

            var res = await _reviews.GradeAsync(userId, subId, score, comment);
            await _max.SendTextAsync(userId, res.Ok ? "Оценка сохранена ✅" : ("Не удалось: " + res.Error));

            if (res.Ok && await _reviews.IsSessionActiveAsync(userId, res.AssignmentId!.Value))
            {
                // подкинем следующую
                await _reviews.SendNextForReviewAsync(userId, res.AssignmentId!.Value);
            }

            return Ok();
        }

        // Неизвестная команда
        await _max.SendTextAsync(userId, "Не знаю такую команду. Попробуйте /help.");
        return Ok();
    }

    private static string? ExtractUrl(string text)
    {
        // очень простой парсер URL в тексте
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (Uri.TryCreate(p, UriKind.Absolute, out var _)) return p;
        }
        return null;
    }
}
