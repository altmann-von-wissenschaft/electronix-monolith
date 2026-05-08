namespace Application.Services;

public interface IPushNotificationSender
{
    Task NotifyOrderStatusChangedAsync(Guid userId, Guid orderId, string statusKey, CancellationToken ct = default);
    Task NotifySupportReplyAsync(Guid customerUserId, Guid questionId, string subject, CancellationToken ct = default);
    Task NotifyNewSupportQuestionForStaffAsync(Guid questionId, string subject, CancellationToken ct = default);
    Task NotifyPendingReviewForModeratorsAsync(Guid reviewId, CancellationToken ct = default);
}
