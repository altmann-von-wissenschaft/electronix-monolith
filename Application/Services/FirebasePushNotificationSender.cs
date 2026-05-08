using Domain.Orders;
using Domain.Users;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class FirebasePushNotificationSender : IPushNotificationSender
{
    private readonly UsersDbContext _users;

    public FirebasePushNotificationSender(UsersDbContext users) => _users = users;

    public async Task NotifyOrderStatusChangedAsync(Guid userId, Guid orderId, string statusKey, CancellationToken ct = default)
    {
        if (FirebaseApp.DefaultInstance == null) return;
        if (!await AllowsOrderStatusAsync(userId, ct)) return;
        var tokens = await TokensForUserAsync(userId, ct);
        if (tokens.Count == 0) return;
        var statusRu = OrderStatusLabelRu(statusKey);
        await SendToTokensAsync(
            tokens,
            "Заказ",
            $"Статус заказа: {statusRu}",
            new Dictionary<string, string>
            {
                ["type"] = "order_status",
                ["orderId"] = orderId.ToString(),
                ["status"] = statusKey,
            },
            ct);
    }

    public async Task NotifySupportReplyAsync(Guid customerUserId, Guid questionId, string subject, CancellationToken ct = default)
    {
        if (FirebaseApp.DefaultInstance == null) return;
        if (!await AllowsSupportReplyAsync(customerUserId, ct)) return;
        var tokens = await TokensForUserAsync(customerUserId, ct);
        if (tokens.Count == 0) return;
        var preview = subject.Length > 80 ? subject[..80] + "…" : subject;
        await SendToTokensAsync(
            tokens,
            "Поддержка",
            $"Новый ответ по обращению: {preview}",
            new Dictionary<string, string>
            {
                ["type"] = "support_reply",
                ["questionId"] = questionId.ToString(),
            },
            ct);
    }

    public async Task NotifyNewSupportQuestionForStaffAsync(Guid questionId, string subject, CancellationToken ct = default)
    {
        if (FirebaseApp.DefaultInstance == null) return;
        var staffUserIds = await _users.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Code == "MANAGER" || ur.Role.Code == "ADMINISTRATOR")
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);

        var tokens = new List<string>();
        foreach (var uid in staffUserIds)
        {
            if (!await AllowsSupportQueueAsync(uid, ct)) continue;
            tokens.AddRange(await TokensForUserAsync(uid, ct));
        }

        tokens = tokens.Distinct().ToList();
        if (tokens.Count == 0) return;
        var preview = subject.Length > 80 ? subject[..80] + "…" : subject;
        await SendToTokensAsync(
            tokens,
            "Очередь поддержки",
            $"Новое обращение: {preview}",
            new Dictionary<string, string>
            {
                ["type"] = "support_queue",
                ["questionId"] = questionId.ToString(),
            },
            ct);
    }

    public async Task NotifyPendingReviewForModeratorsAsync(Guid reviewId, CancellationToken ct = default)
    {
        if (FirebaseApp.DefaultInstance == null) return;
        var modUserIds = await _users.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Code == "MODERATOR" || ur.Role.Code == "ADMINISTRATOR")
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);

        var tokens = new List<string>();
        foreach (var uid in modUserIds)
        {
            if (!await AllowsReviewModerationAsync(uid, ct)) continue;
            tokens.AddRange(await TokensForUserAsync(uid, ct));
        }

        tokens = tokens.Distinct().ToList();
        if (tokens.Count == 0) return;
        await SendToTokensAsync(
            tokens,
            "Модерация",
            "Новый отзыв ожидает проверки",
            new Dictionary<string, string>
            {
                ["type"] = "review_moderation",
                ["reviewId"] = reviewId.ToString(),
            },
            ct);
    }

    private async Task<bool> AllowsOrderStatusAsync(Guid userId, CancellationToken ct)
    {
        var p = await _users.UserPushPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return p == null || p.NotifyOrderStatus;
    }

    private async Task<bool> AllowsSupportReplyAsync(Guid userId, CancellationToken ct)
    {
        var p = await _users.UserPushPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return p == null || p.NotifySupportReply;
    }

    private async Task<bool> AllowsSupportQueueAsync(Guid userId, CancellationToken ct)
    {
        var p = await _users.UserPushPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return p == null || p.NotifySupportQueue;
    }

    private async Task<bool> AllowsReviewModerationAsync(Guid userId, CancellationToken ct)
    {
        var p = await _users.UserPushPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return p == null || p.NotifyReviewModeration;
    }

    private static string OrderStatusLabelRu(string statusKey)
    {
        if (!Enum.TryParse<OrderStatus>(statusKey, true, out var s))
            return statusKey;
        return s switch
        {
            OrderStatus.Pending => "ожидает обработки",
            OrderStatus.Processing => "в обработке",
            OrderStatus.ReadyForPickup => "готов к выдаче",
            OrderStatus.Completed => "выполнен",
            OrderStatus.Cancelled => "отменён",
            _ => statusKey,
        };
    }

    private async Task<List<string>> TokensForUserAsync(Guid userId, CancellationToken ct) =>
        await _users.FcmDeviceRegistrations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Token)
            .ToListAsync(ct);

    private static async Task SendToTokensAsync(
        List<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken ct)
    {
        const int batchSize = 500;
        for (var i = 0; i < tokens.Count; i += batchSize)
        {
            var chunk = tokens.GetRange(i, Math.Min(batchSize, tokens.Count - i));
            var message = new MulticastMessage
            {
                Tokens = chunk,
                Notification = new Notification { Title = title, Body = body },
                Data = data.ToDictionary(kv => kv.Key, kv => kv.Value),
                Android = new AndroidConfig { Priority = Priority.High },
            };
            _ = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken: ct);
        }
    }
}
