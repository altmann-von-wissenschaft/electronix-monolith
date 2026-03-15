using Application.DTOs.Products;
using Domain.Products;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Products
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ProductsDbContext _context;

        public CategoriesController(ProductsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all categories (hierarchical)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] Guid? parentId = null)
        {
            var query = _context.Categories.AsQueryable();

            if (parentId.HasValue)
                query = query.Where(c => c.ParentId == parentId.Value);
            else
                query = query.Where(c => c.ParentId == null);  // Top-level only

            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var dtos = categories.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Get category by ID with subcategories
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return Ok(MapToDto(category));
        }

        /// <summary>
        /// Create category (admin only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto request)
        {
            if (request.ParentId.HasValue)
            {
                var parent = await _context.Categories.FindAsync(request.ParentId.Value);
                if (parent == null)
                    return BadRequest(new { message = "Parent category not found" });
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ParentId = request.ParentId,
                DisplayOrder = request.DisplayOrder
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToDto(category));
        }

        /// <summary>
        /// Update category (admin only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryDto request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            if (!string.IsNullOrEmpty(request.Name))
                category.Name = request.Name;

            if (request.Description != null)
                category.Description = request.Description;

            if (request.ParentId.HasValue && request.ParentId.Value != id)
            {
                var parent = await _context.Categories.FindAsync(request.ParentId.Value);
                if (parent == null)
                    return BadRequest(new { message = "Parent category not found" });
                category.ParentId = request.ParentId.Value;
            }

            category.DisplayOrder = request.DisplayOrder;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(category));
        }

        /// <summary>
        /// Delete category - only if no products assigned (admin only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            if (category.Products.Any() || category.Children.Any())
                return BadRequest(new { message = "Cannot delete category with products or subcategories" });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted" });
        }

        private CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId,
                DisplayOrder = category.DisplayOrder
            };
        }
    }
}
