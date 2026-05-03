using Application.DTOs.Products;
using Domain.Products;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Products
{
    [ApiController]
    [Route("api/categories/{categoryId}/characteristics")]
    [Authorize(Roles = "ADMINISTRATOR")]
    public class CategoryCharacteristicsController : ControllerBase
    {
        private readonly ProductsDbContext _context;

        public CategoryCharacteristicsController(ProductsDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetCategoryCharacteristics(Guid categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                return NotFound(new { message = "Category not found" });

            var characteristics = await _context.CategoryCharacteristics
                .Where(cc => cc.CategoryId == categoryId)
                .Include(cc => cc.Characteristic)
                .OrderBy(cc => cc.Characteristic.Name)
                .ToListAsync();

            var dtos = characteristics.Select(cc => new CategoryCharacteristicDto
            {
                Id = cc.Id,
                CharacteristicId = cc.CharacteristicId,
                CharacteristicName = cc.Characteristic.Name,
                Unit = cc.Characteristic.Unit,
                IsRequired = cc.IsRequired
            }).ToList();

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> AssignCharacteristic(Guid categoryId, [FromBody] AssignCharacteristicRequest request)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                return NotFound(new { message = "Category not found" });

            var characteristic = await _context.Characteristics.FindAsync(request.CharacteristicId);
            if (characteristic == null)
                return NotFound(new { message = "Characteristic not found" });

            var existing = await _context.CategoryCharacteristics
                .FirstOrDefaultAsync(cc => cc.CategoryId == categoryId && cc.CharacteristicId == request.CharacteristicId);

            if (existing != null)
                return BadRequest(new { message = "Characteristic already assigned to this category" });

            var categoryCharacteristic = new CategoryCharacteristic
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                CharacteristicId = request.CharacteristicId,
                IsRequired = request.IsRequired
            };

            _context.CategoryCharacteristics.Add(categoryCharacteristic);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategoryCharacteristics), new { categoryId }, new CategoryCharacteristicDto
            {
                Id = categoryCharacteristic.Id,
                CharacteristicId = characteristic.Id,
                CharacteristicName = characteristic.Name,
                Unit = characteristic.Unit,
                IsRequired = categoryCharacteristic.IsRequired
            });
        }

        [HttpPut("{characteristicId}")]
        public async Task<IActionResult> UpdateCharacteristicAssignment(Guid categoryId, Guid characteristicId, [FromBody] UpdateCharacteristicAssignmentRequest request)
        {
            var categoryCharacteristic = await _context.CategoryCharacteristics
                .FirstOrDefaultAsync(cc => cc.CategoryId == categoryId && cc.CharacteristicId == characteristicId);

            if (categoryCharacteristic == null)
                return NotFound(new { message = "Characteristic not assigned to this category" });

            categoryCharacteristic.IsRequired = request.IsRequired;

            await _context.SaveChangesAsync();

            var characteristic = await _context.Characteristics.FindAsync(characteristicId);

            return Ok(new CategoryCharacteristicDto
            {
                Id = categoryCharacteristic.Id,
                CharacteristicId = characteristicId,
                CharacteristicName = characteristic!.Name,
                Unit = characteristic.Unit,
                IsRequired = categoryCharacteristic.IsRequired
            });
        }

        [HttpDelete("{characteristicId}")]
        public async Task<IActionResult> UnassignCharacteristic(Guid categoryId, Guid characteristicId)
        {
            var categoryCharacteristic = await _context.CategoryCharacteristics
                .FirstOrDefaultAsync(cc => cc.CategoryId == categoryId && cc.CharacteristicId == characteristicId);

            if (categoryCharacteristic == null)
                return NotFound(new { message = "Characteristic not assigned to this category" });

            _context.CategoryCharacteristics.Remove(categoryCharacteristic);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Characteristic unassigned successfully" });
        }
    }
}
