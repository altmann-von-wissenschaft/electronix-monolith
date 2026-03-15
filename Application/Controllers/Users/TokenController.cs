using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.DTOs.Users;
using Application.Services;
using Domain.Users;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Application.Controllers.Users
{
    [ApiController]
    [Route("api/users")]
    public class TokenController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly UsersDbContext _usersContext;
        private readonly CartDbContext _cartContext;

        public TokenController(AuthService authService, UsersDbContext usersContext, CartDbContext cartContext)
        {
            _authService = authService;
            _usersContext = usersContext;
            _cartContext = cartContext;
        }

        /// <summary>
        /// Register a new user with guest role
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email and password are required" });

            // Check if user already exists
            if (await _usersContext.Users.AnyAsync(u => u.Email == request.Email))
                return Conflict(new { message = "User with this email already exists" });

            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    PasswordHash = AuthService.HashPassword(request.Password),
                    Nickname = request.Nickname,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _usersContext.Users.Add(user);

                // Assign guest role
                var guestRole = await _usersContext.Roles.FirstOrDefaultAsync(r => r.Code == "GUEST");
                if (guestRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = guestRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _usersContext.UserRoles.Add(userRole);
                }

                await _usersContext.SaveChangesAsync();

                // Create cart for user in CartDbContext
                var cart = new Domain.Cart.Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _cartContext.Carts.Add(cart);
                await _cartContext.SaveChangesAsync();

                return Ok(new { message = "User registered successfully", userId = user.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error registering user", error = ex.Message });
            }
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email and password are required" });

            var result = await _authService.AuthenticateAsync(request.Email, request.Password);

            if (result == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(new LoginResponse
            {
                Token = result.Token,
                UserId = result.UserId,
                Email = result.Email,
                Nickname = result.Nickname,
                Roles = result.Roles
            });
        }

        /// <summary>
        /// Get current user info (requires authentication)
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var user = await _usersContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Nickname = user.Nickname,
                IsBlocked = user.IsBlocked,
                Roles = user.UserRoles.Select(ur => ur.Role.Code).ToList()
            });
        }

        /// <summary>
        /// Refresh token - generates new token based on current authentication
        /// </summary>
        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var user = await _usersContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || user.IsBlocked)
                return Unauthorized();

            var token = _authService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}