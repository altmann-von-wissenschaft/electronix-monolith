using Application.DTOs.Users;
using Domain.Users;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Users
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize] // per-action: list/roles = admin only; lookup + block = moderator or admin
    public class AdminUsersController : ControllerBase
    {
        private readonly UsersDbContext _context;

        public AdminUsersController(UsersDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all users (admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMINISTRATOR")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = users.Select(MapToDto).ToList();
            return Ok(new { data = dtos, page, pageSize });
        }

        /// <summary>
        /// Get user by ID (admin only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "MODERATOR,ADMINISTRATOR")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            return Ok(MapToDto(user));
        }

        /// <summary>
        /// Block/Unblock user (admin only)
        /// </summary>
        [HttpPost("{id}/block")]
        [Authorize(Roles = "MODERATOR,ADMINISTRATOR")]
        public async Task<IActionResult> BlockUser(Guid id, [FromBody] bool block)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();

            // Moderators cannot block administrator accounts
            if (!User.IsInRole("ADMINISTRATOR") && User.IsInRole("MODERATOR"))
            {
                if (user.UserRoles.Any(ur => ur.Role.Code == "ADMINISTRATOR"))
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "Moderators cannot block administrator accounts" });
            }

            user.IsBlocked = block;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = block ? "User blocked" : "User unblocked", user = MapToDto(user) });
        }

        /// <summary>
        /// Assign role to user (admin only)
        /// </summary>
        [HttpPost("{id}/roles")]
        [Authorize(Roles = "ADMINISTRATOR")]
        public async Task<IActionResult> AssignRole(Guid id, [FromBody] string roleCode)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);
            if (role == null)
                return BadRequest(new { message = "Role not found" });

            // Check if user already has this role
            if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
                return BadRequest(new { message = "User already has this role" });

            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow
            };

            user.UserRoles.Add(userRole);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Role assigned", user = MapToDto(user) });
        }

        /// <summary>
        /// Remove role from user (admin only)
        /// </summary>
        [HttpDelete("{id}/roles/{roleCode}")]
        [Authorize(Roles = "ADMINISTRATOR")]
        public async Task<IActionResult> RemoveRole(Guid id, string roleCode)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var userRole = user.UserRoles.FirstOrDefault(ur => ur.Role.Code == roleCode);
            if (userRole == null)
                return BadRequest(new { message = "User doesn't have this role" });

            // Prevent removing all roles
            if (user.UserRoles.Count == 1)
                return BadRequest(new { message = "User must have at least one role" });

            _context.UserRoles.Remove(userRole);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Role removed", user = MapToDto(user) });
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Nickname = user.Nickname,
                IsBlocked = user.IsBlocked,
                Roles = user.UserRoles.Select(ur => ur.Role.Code).ToList()
            };
        }
    }
}
