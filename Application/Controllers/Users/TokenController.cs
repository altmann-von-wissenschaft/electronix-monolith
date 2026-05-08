using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.DTOs.Users;
using Application.Services;
using Domain.Users;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Application.Controllers.Users
{
    [ApiController]
    [Route("api/users")]
    public class TokenController : ControllerBase
    {
    private static readonly Regex EmailRegex = new(
        @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex NicknameRegex = new(
        @"^[A-Za-zА-Яа-яЁё][A-Za-zА-Яа-яЁё0-9_]{3,23}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

            var email = request.Email.Trim();
            var password = request.Password;
            var nickname = request.Nickname?.Trim();
            if (!EmailRegex.IsMatch(email))
                return BadRequest(new { message = "Некорректный формат email." });
            if (!IsPasswordComplex(password))
                return BadRequest(new { message = "Пароль должен быть не короче 8 символов и содержать заглавную, строчную буквы и цифру." });
            if (!string.IsNullOrWhiteSpace(nickname) && !NicknameRegex.IsMatch(nickname))
                return BadRequest(new { message = "Псевдоним: 4-24 символа, начинается с буквы, допустимы кириллица/латиница, цифры и _." });

            // Check if user already exists
            if (await _usersContext.Users.AnyAsync(u => u.Email == email))
                return Conflict(new { message = "User with this email already exists" });

            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = AuthService.HashPassword(password),
                    Nickname = nickname,
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

            var result = await _authService.AuthenticateAsync(request.Email.Trim(), request.Password);

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

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue) return Unauthorized();
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
            {
                return BadRequest(new { message = "Заполните все поля пароля." });
            }
            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest(new { message = "Новый пароль и подтверждение не совпадают." });
            if (!IsPasswordComplex(request.NewPassword))
                return BadRequest(new { message = "Пароль должен быть не короче 8 символов и содержать заглавную, строчную буквы и цифру." });
            var user = await _usersContext.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null || user.IsBlocked) return Unauthorized();
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Текущий пароль указан неверно." });
            user.PasswordHash = AuthService.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _usersContext.SaveChangesAsync();
            return Ok(new { message = "Пароль изменен." });
        }

        private static bool IsPasswordComplex(string password)
        {
            if (password.Length < 8 || password.Length > 72) return false;
            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            return hasUpper && hasLower && hasDigit;
        }
    }
}