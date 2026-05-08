namespace Application.Services;

public sealed class NullPushNotificationSender : IPushNotificationSender
{
    public Task NotifyOrderStatusChangedAsync(Guid userId, Guid orderId, string statusKey, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifySupportReplyAsync(Guid customerUserId, Guid questionId, string subject, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifyNewSupportQuestionForStaffAsync(Guid questionId, string subject, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifyPendingReviewForModeratorsAsync(Guid reviewId, CancellationToken ct = default) =>
        Task.CompletedTask;
}
