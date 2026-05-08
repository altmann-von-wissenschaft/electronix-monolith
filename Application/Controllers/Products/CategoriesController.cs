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
                .AsNoTracking()
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
                .AsNoTracking()
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Characteristics)
                .ThenInclude(cc => cc.Characteristic)
                .AsSplitQuery()
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
                ParentId = request.ParentId,
                DisplayOrder = request.DisplayOrder
            };

            _context.Categories.Add(category);

            if (request.Characteristics is { Count: > 0 })
            {
                var err = await ApplyCategoryCharacteristicsAsync(category.Id, request.Characteristics);
                if (err != null)
                {
                    return err;
                }
            }

            await _context.SaveChangesAsync();

            var created = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Characteristics)
                .ThenInclude(cc => cc.Characteristic)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == category.Id);

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToDto(created!));
        }

        /// <summary>
        /// Update category (admin only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (category == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(request.Name))
                    category.Name = request.Name;

                if (request.ParentId.HasValue && request.ParentId.Value != id)
                {
                    var parent = await _context.Categories.FindAsync(request.ParentId.Value);
                    if (parent == null)
                        return BadRequest(new { message = "Parent category not found" });
                    category.ParentId = request.ParentId.Value;
                }

                if (request.DisplayOrder.HasValue)
                    category.DisplayOrder = request.DisplayOrder.Value;

                if (request.Characteristics != null)
                {
                    var err = await ApplyCategoryCharacteristicsAsync(category.Id, request.Characteristics);
                    if (err != null)
                        return err;
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var stillExists = await _context.Categories.AnyAsync(c => c.Id == id);
                if (!stillExists)
                    return NotFound(new { message = "Category not found" });

                return Conflict(new
                {
                    message = "Category was modified by another request. Please refresh and retry."
                });
            }

            var updatedCategory = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Characteristics)
                .ThenInclude(cc => cc.Characteristic)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (updatedCategory == null)
                return NotFound(new { message = "Category not found" });

            return Ok(MapToDto(updatedCategory));
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
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            if (category.Products.Any() || category.Children.Any())
                return BadRequest(new { message = "Cannot delete category with products or subcategories" });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted" });
        }

        private async Task<IActionResult?> ApplyCategoryCharacteristicsAsync(
            Guid categoryId,
            List<AssignCharacteristicRequest> characteristics)
        {
            var duplicateCharacteristicId = characteristics
                .GroupBy(c => c.CharacteristicId)
                .FirstOrDefault(g => g.Count() > 1)?.Key;

            if (duplicateCharacteristicId.HasValue)
            {
                return BadRequest(new
                {
                    message = $"Duplicate characteristicId '{duplicateCharacteristicId.Value}' in request body."
                });
            }

            var requestedCharacteristicIds = characteristics
                .Select(c => c.CharacteristicId)
                .ToHashSet();

            var categoryCharacteristics = await _context.CategoryCharacteristics
                .Where(cc => cc.CategoryId == categoryId)
                .ToListAsync();

            var knownCharacteristicIds = await _context.Characteristics
                .Where(c => requestedCharacteristicIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            var missingCharacteristicIds = requestedCharacteristicIds
                .Except(knownCharacteristicIds)
                .ToList();

            if (missingCharacteristicIds.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Some characteristicIds were not found",
                    characteristicIds = missingCharacteristicIds
                });
            }

            var toRemove = categoryCharacteristics
                .Where(cc => !requestedCharacteristicIds.Contains(cc.CharacteristicId))
                .ToList();

            if (toRemove.Count > 0)
                _context.CategoryCharacteristics.RemoveRange(toRemove);

            foreach (var requestCharacteristic in characteristics)
            {
                var existing = categoryCharacteristics
                    .FirstOrDefault(cc => cc.CharacteristicId == requestCharacteristic.CharacteristicId);

                if (existing == null)
                {
                    _context.CategoryCharacteristics.Add(new CategoryCharacteristic
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = categoryId,
                        CharacteristicId = requestCharacteristic.CharacteristicId,
                        IsRequired = requestCharacteristic.IsRequired
                    });
                }
                else
                {
                    existing.IsRequired = requestCharacteristic.IsRequired;
                }
            }

            return null;
        }

        private CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId,
                DisplayOrder = category.DisplayOrder,
                Characteristics = category.Characteristics.OrderBy(c => c.Characteristic.Name)
                    .Select(cc => new CategoryCharacteristicDto
                    {
                        Id = cc.Id,
                        CharacteristicId = cc.CharacteristicId,
                        CharacteristicName = cc.Characteristic.Name,
                        Unit = cc.Characteristic.Unit,
                        IsRequired = cc.IsRequired
                    }).ToList()
            };
        }
    }
}
