namespace Domain.Users;

public class UserPushPreferences
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool NotifyOrderStatus { get; set; } = true;
    public bool NotifySupportReply { get; set; } = true;
    public bool NotifyReviewModeration { get; set; } = true;
    public bool NotifySupportQueue { get; set; } = true;
}
