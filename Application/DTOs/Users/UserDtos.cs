namespace Application.DTOs.Users;

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Nickname { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string? Nickname { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Nickname { get; set; }
    public bool IsBlocked { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class PutFcmTokenRequest
{
    public string Token { get; set; } = null!;
}

public class PutPushPreferencesRequest
{
    public bool NotifyOrderStatus { get; set; }
    public bool NotifySupportReply { get; set; }
    public bool NotifyReviewModeration { get; set; }
    public bool NotifySupportQueue { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmNewPassword { get; set; } = null!;
}
