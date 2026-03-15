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
        private readonly ReviewsDbContext _context;
        private readonly ProductsService _productsService;

        public ReviewsController(ReviewsDbContext context, ProductsService productsService)
        {
            _context = context;
            _productsService = productsService;
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
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = reviews.Select(MapToDto).ToList();
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
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = reviews.Select(MapToDto).ToList();
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
                Title = request.Title,
                Content = request.Content,
                IsApproved = false,  // Auto-approve for now, can be changed
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviews), new { productId = request.ProductId }, MapToDto(review));
        }

        /// <summary>
        /// Delete review (moderator/admin only)
        /// </summary>
        [Authorize(Roles = "MODERATOR,ADMINISTRATOR")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review deleted" });
        }

        /// <summary>
        /// Approve review (moderator/admin only)
        /// </summary>
        [Authorize(Roles = "MODERATOR,ADMINISTRATOR")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReview(Guid id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            review.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok(MapToDto(review));
        }

        private ReviewDto MapToDto(Review review)
        {
            return new ReviewDto
            {
                Id = review.Id,
                ProductId = review.ProductId,
                UserId = review.UserId,
                Rating = review.Rating,
                Title = review.Title,
                Content = review.Content,
                IsApproved = review.IsApproved,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
