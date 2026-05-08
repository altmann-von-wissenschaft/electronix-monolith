using Application.DTOs.Reviews;
using Application.Services;
using Domain.Reviews;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Reviews
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private const int ReviewTitleMin = 4;
        private const int ReviewTitleMax = 120;
        private const int ReviewContentMin = 15;
        private const int ReviewContentMax = 2000;
        private readonly ReviewsDbContext _context;
        private readonly UsersDbContext _usersContext;
        private readonly ProductsService _productsService;
        private readonly IPushNotificationSender _push;

        public ReviewsController(
            ReviewsDbContext context,
            UsersDbContext usersContext,
            ProductsService productsService,
            IPushNotificationSender push)
        {
            _context = context;
            _usersContext = usersContext;
            _productsService = productsService;
            _push = push;
        }

        /// <summary>
        /// Get product reviews
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] Guid? productId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Reviews.Where(r => r.IsApproved);

            if (productId.HasValue)
                query = query.Where(r => r.ProductId == productId.Value);

            var reviews = await query
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = await MapToDtoListAsync(reviews);
            return Ok(new { data = dtos, page, pageSize });
        }

        /// <summary>
        /// Get recent unapproved reviews (moderator only)
        /// </summary>
        [Authorize(Roles = "MODERATOR,ADMINISTRATOR")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = await MapToDtoListAsync(reviews);
            return Ok(new { data = dtos, page, pageSize });
        }

        /// <summary>
        /// Create review for product (authenticated users)
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5" });
            var title = request.Title?.Trim() ?? "";
            var content = request.Content?.Trim() ?? "";
            if (title.Length < ReviewTitleMin || title.Length > ReviewTitleMax)
                return BadRequest(new { message = $"Заголовок отзыва: от {ReviewTitleMin} до {ReviewTitleMax} символов." });
            if (content.Length < ReviewContentMin || content.Length > ReviewContentMax)
                return BadRequest(new { message = $"Текст отзыва: от {ReviewContentMin} до {ReviewContentMax} символов." });

            // Call ProductsService to validate product exists
            var product = await _productsService.GetProductAsync(request.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var review = new Review
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                UserId = userId.Value,
                Rating = request.Rating,
                Title = title,
                Content = content,
                IsApproved = false,  // Auto-approve for now, can be changed
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            await _push.NotifyPendingReviewForModeratorsAsync(review.Id, HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetReviews), new { productId = request.ProductId }, await MapToDtoAsync(review));
        }

        /// <summary>
        /// Delete review (moderator/admin only)
        /// </summary>
        [Authorize(Roles = "MODERATOR,ADMINISTRATOR")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var deleted = await _context.Reviews
                .Where(r => r.Id == id && !r.IsApproved)
                .ExecuteDeleteAsync();

            if (deleted == 0)
            {
                if (!await _context.Reviews.AnyAsync(r => r.Id == id))
                    return NotFound();
                return Conflict(new { message = "Review is no longer pending (already approved or removed)." });
            }

            return Ok(new { message = "Review deleted" });
        }

        /// <summary>
        /// Approve review (moderator/admin only)
        /// </summary>
        [Authorize(Roles = "MODERATOR,ADMINISTRATOR")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReview(Guid id)
        {
            var updated = await _context.Reviews
                .Where(r => r.Id == id && !r.IsApproved)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsApproved, true));

            if (updated == 0)
            {
                if (!await _context.Reviews.AnyAsync(r => r.Id == id))
                    return NotFound();
                return Conflict(new { message = "Review is no longer pending (already processed by another moderator)." });
            }

            var review = await _context.Reviews.FirstAsync(r => r.Id == id);
            return Ok(await MapToDtoAsync(review));
        }

        private async Task<Dictionary<Guid, string?>> LoadAuthorNicknamesAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, string?>();

            return await _usersContext.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Nickname);
        }

        private async Task<List<ReviewDto>> MapToDtoListAsync(List<Review> reviews)
        {
            var nicknames = await LoadAuthorNicknamesAsync(reviews.Select(r => r.UserId));
            return reviews.Select(r => MapToDto(r, nicknames.GetValueOrDefault(r.UserId))).ToList();
        }

        private async Task<ReviewDto> MapToDtoAsync(Review review)
        {
            var nicknames = await LoadAuthorNicknamesAsync(new[] { review.UserId });
            return MapToDto(review, nicknames.GetValueOrDefault(review.UserId));
        }

        private static ReviewDto MapToDto(Review review, string? authorNickname)
        {
            return new ReviewDto
            {
                Id = review.Id,
                ProductId = review.ProductId,
                UserId = review.UserId,
                AuthorNickname = authorNickname,
                Rating = review.Rating,
                Title = review.Title,
                Content = review.Content,
                IsApproved = review.IsApproved,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
