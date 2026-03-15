using System.Security.Claims;
using System.Text;

namespace Application;

public class AuthToken()
{
    public static readonly byte[] key = Encoding.ASCII.GetBytes("7ntbLwQvepRN4X1Uv9o7m29DnPclkL7adynTm2ex");

    public static bool VerifyID(ClaimsPrincipal principal, Guid id)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return false;
        
        return id == userId;
    }

    public static Guid? GetID(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public static string? GetEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static string? GetNickname(ClaimsPrincipal principal)
    {
        return principal.FindFirst("nickname")?.Value;
    }

    public static IEnumerable<string> GetRoles(ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    public static int? GetHierarchy(ClaimsPrincipal principal)
    {
        var hierarchyClaim = principal.FindFirst("hierarchy")?.Value;
        return int.TryParse(hierarchyClaim, out var hierarchy) ? hierarchy : null;
    }
}