using Application.DTOs.Products;
using Domain.Products;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Products
{
    [ApiController]
    [Route("api/characteristics")]
    public class CharacteristicsController : ControllerBase
    {
        private readonly ProductsDbContext _context;

        public CharacteristicsController(ProductsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCharacteristics([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var characteristics = await _context.Characteristics
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = characteristics.Select(c => new CharacteristicDto
            {
                Id = c.Id,
                Name = c.Name,
                Unit = c.Unit
            }).ToList();

            return Ok(new { data = dtos, page, pageSize });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCharacteristic(Guid id)
        {
            var characteristic = await _context.Characteristics.FindAsync(id);
            if (characteristic == null)
                return NotFound();

            return Ok(new CharacteristicDto
            {
                Id = characteristic.Id,
                Name = characteristic.Name,
                Unit = characteristic.Unit
            });
        }

        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPost]
        public async Task<IActionResult> CreateCharacteristic([FromBody] CreateCharacteristicRequest request)
        {
            var characteristic = new Characteristic
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Unit = request.Unit
            };

            _context.Characteristics.Add(characteristic);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCharacteristic), new { id = characteristic.Id }, new CharacteristicDto
            {
                Id = characteristic.Id,
                Name = characteristic.Name,
                Unit = characteristic.Unit
            });
        }

        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCharacteristic(Guid id, [FromBody] CreateCharacteristicRequest request)
        {
            var characteristic = await _context.Characteristics.FindAsync(id);
            if (characteristic == null)
                return NotFound();

            characteristic.Name = request.Name;
            characteristic.Unit = request.Unit;

            await _context.SaveChangesAsync();

            return Ok(new CharacteristicDto
            {
                Id = characteristic.Id,
                Name = characteristic.Name,
                Unit = characteristic.Unit
            });
        }
    }
}
