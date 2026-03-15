using Application.DTOs.Products;
using Application.Services;
using Domain.Products;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Products
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductsDbContext _context;
        private readonly MinioService _minioService;

        public ProductsController(ProductsDbContext context, MinioService minioService)
        {
            _context = context;
            _minioService = minioService;
        }

        /// <summary>
        /// Get all non-hidden products
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] Guid? categoryId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Products.Where(p => !p.IsHidden);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Category)
                .Include(p => p.Attributes)
                .Include(p => p.Images)
                .ToListAsync();

            var dtos = products.Select(p => MapToDto(p)).ToList();
            return Ok(new { data = dtos, page, pageSize });
        }

        /// <summary>
        /// Get single product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Attributes)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null || product.IsHidden)
                return NotFound();

            return Ok(MapToDto(product));
        }

        /// <summary>
        /// Create new product (administrator only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var category = await _context.Categories.FindAsync(request.CategoryId);
            if (category == null)
                return BadRequest(new { message = "Category not found" });

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);

            if (request.Attributes.Any())
            {
                foreach (var attr in request.Attributes)
                {
                    _context.ProductAttributes.Add(new ProductAttribute
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Name = attr.Name,
                        Value = attr.Value,
                        Unit = attr.Unit
                    });
                }
            }

            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, MapToDto(product));
        }

        /// <summary>
        /// Update product (administrator only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            if (!string.IsNullOrEmpty(request.Name))
                product.Name = request.Name;

            if (request.Description != null)
                product.Description = request.Description;

            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            if (request.Stock.HasValue)
                product.Stock = request.Stock.Value;

            if (request.CategoryId.HasValue)
            {
                var category = await _context.Categories.FindAsync(request.CategoryId.Value);
                if (category == null)
                    return BadRequest(new { message = "Category not found" });
                product.CategoryId = request.CategoryId.Value;
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(MapToDto(product));
        }

        /// <summary>
        /// Hide product from catalog (administrator only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPost("{id}/hide")]
        public async Task<IActionResult> HideProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsHidden = true;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product hidden successfully" });
        }

        /// <summary>
        /// Show product in catalog (administrator only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPost("{id}/show")]
        public async Task<IActionResult> ShowProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsHidden = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product shown successfully" });
        }

        /// <summary>
        /// Upload product image (administrator only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadProductImage(Guid id, IFormFile file)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            try
            {
                var fileName = await _minioService.UploadProductImageAsync(file, id);

                var productImage = new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = id,
                    ObjectName = fileName,
                    DisplayOrder = (_context.ProductImages.Where(pi => pi.ProductId == id).Max(pi => (int?)pi.DisplayOrder) ?? 0) + 1,
                    UploadedAt = DateTime.UtcNow
                };

                _context.ProductImages.Add(productImage);

                if (product.MainImagePath == null)
                    product.MainImagePath = fileName;

                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Image uploaded successfully", fileName = fileName, imageId = productImage.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to upload image", error = ex.Message });
            }
        }

        private ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                IsHidden = product.IsHidden,
                MainImagePath = product.MainImagePath,
                CategoryId = product.CategoryId,
                Attributes = product.Attributes.Select(a => new ProductAttributeDto
                {
                    Name = a.Name,
                    Value = a.Value,
                    Unit = a.Unit
                }).ToList(),
                ImagePaths = product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ObjectName).ToList()
            };
        }
    }
}
