using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infrastructure.Contexts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services;

public class AuthService
{
    private readonly UsersDbContext _context;
    private readonly SymmetricSecurityKey _key;
    private readonly SigningCredentials _credentials;

    public AuthService(UsersDbContext context)
    {
        _context = context;
        _key = new SymmetricSecurityKey(AuthToken.key);
        _credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token with role claims
    /// </summary>
    public async Task<AuthTokenResponse?> AuthenticateAsync(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !VerifyPassword(password, user.PasswordHash))
            return null;

        if (user.IsBlocked)
            return null;

        var token = GenerateToken(user);
        return new AuthTokenResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Nickname = user.Nickname,
            Roles = user.UserRoles.Select(ur => ur.Role.Code).ToList()
        };
    }

    /// <summary>
    /// Generates JWT token with user claims and roles
    /// </summary>
    public string GenerateToken(User user)
    {
        var roles = _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToList();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("nickname", user.Nickname ?? ""),
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Code));
        }

        // Add highest permission level claim for role hierarchy
        var highestHierarchy = roles.Max(r => (int?)r.Hierarchy) ?? 0;
        claims.Add(new Claim("hierarchy", highestHierarchy.ToString()));

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: _credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

public class AuthTokenResponse
{
    public string Token { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string? Nickname { get; set; }
    public List<string> Roles { get; set; } = new();
}
