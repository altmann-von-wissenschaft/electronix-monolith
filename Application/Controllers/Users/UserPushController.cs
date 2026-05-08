using Application;
using Application.DTOs.Users;
using Domain.Users;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Users;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserPushController : ControllerBase
{
    private readonly UsersDbContext _users;

    public UserPushController(UsersDbContext users) => _users = users;

    /// <summary>Register or replace FCM token for this device (one token may only belong to one user).</summary>
    [HttpPut("me/fcm-token")]
    public async Task<IActionResult> PutFcmToken([FromBody] PutFcmTokenRequest request, CancellationToken ct)
    {
        var userId = AuthToken.GetID(User);
        if (!userId.HasValue)
            return Unauthorized();

        var token = request.Token?.Trim() ?? "";
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Token is required" });

        var existingForToken = await _users.FcmDeviceRegistrations
            .Where(x => x.Token == token)
            .ToListAsync(ct);
        if (existingForToken.Count > 0)
            _users.FcmDeviceRegistrations.RemoveRange(existingForToken);

        _users.FcmDeviceRegistrations.Add(new FcmDeviceRegistration
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Token = token,
            UpdatedAt = DateTime.UtcNow,
        });

        await _users.SaveChangesAsync(ct);
        return Ok(new { message = "Token registered" });
    }

    [HttpDelete("me/fcm-token")]
    public async Task<IActionResult> DeleteFcmToken([FromBody] PutFcmTokenRequest request, CancellationToken ct)
    {
        var userId = AuthToken.GetID(User);
        if (!userId.HasValue)
            return Unauthorized();

        var token = request.Token?.Trim() ?? "";
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Token is required" });

        var deleted = await _users.FcmDeviceRegistrations
            .Where(x => x.UserId == userId.Value && x.Token == token)
            .ExecuteDeleteAsync(ct);

        return Ok(new { removed = deleted });
    }

    /// <summary>Server-side push preferences (must match app toggles for energy-efficient targeting).</summary>
    [HttpPut("me/push-preferences")]
    public async Task<IActionResult> PutPushPreferences([FromBody] PutPushPreferencesRequest body, CancellationToken ct)
    {
        var userId = AuthToken.GetID(User);
        if (!userId.HasValue)
            return Unauthorized();

        var row = await _users.UserPushPreferences.FirstOrDefaultAsync(x => x.UserId == userId.Value, ct);
        if (row == null)
        {
            row = new UserPushPreferences
            {
                UserId = userId.Value,
                NotifyOrderStatus = body.NotifyOrderStatus,
                NotifySupportReply = body.NotifySupportReply,
                NotifyReviewModeration = body.NotifyReviewModeration,
                NotifySupportQueue = body.NotifySupportQueue,
            };
            _users.UserPushPreferences.Add(row);
        }
        else
        {
            row.NotifyOrderStatus = body.NotifyOrderStatus;
            row.NotifySupportReply = body.NotifySupportReply;
            row.NotifyReviewModeration = body.NotifyReviewModeration;
            row.NotifySupportQueue = body.NotifySupportQueue;
        }

        await _users.SaveChangesAsync(ct);
        return Ok(new { message = "Preferences saved" });
    }
}
