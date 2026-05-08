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
        private const string CharacteristicFilterPrefix = "filter.";
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
        public async Task<IActionResult> GetProducts(
            [FromQuery] Guid? categoryId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "page and pageSize must be greater than 0" });

            var filterParseResult = ParseCharacteristicFilters();
            if (!filterParseResult.IsValid)
                return BadRequest(new { message = filterParseResult.ErrorMessage });

            var query = _context.Products.Where(p => !p.IsHidden);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.Name.ToLower().Contains(term.ToLower()));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            foreach (var filter in filterParseResult.Filters)
            {
                var characteristicId = filter.Key;
                var min = filter.Min;
                var max = filter.Max;

                query = query.Where(p => p.CharacteristicValues.Any(cv =>
                    cv.CharacteristicId == characteristicId &&
                    (!min.HasValue || cv.Value >= min.Value) &&
                    (!max.HasValue || cv.Value <= max.Value)));
            }

            var products = await query
                .AsNoTracking()
                .OrderByDescending(p => p.UpdatedAt)
                .ThenBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Category)
                .Include(p => p.CharacteristicValues)
                .ThenInclude(cv => cv.Characteristic)
                .Include(p => p.Images)
                .AsSplitQuery()
                .ToListAsync();

            var dtos = products.Select(p => MapToDto(p)).ToList();
            return Ok(new { data = dtos, page, pageSize });
        }

        private CharacteristicFilterParseResult ParseCharacteristicFilters()
        {
            var filters = new Dictionary<Guid, CharacteristicFilter>();

            foreach (var queryPair in Request.Query)
            {
                var queryKey = queryPair.Key;
                if (!queryKey.StartsWith(CharacteristicFilterPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var suffix = queryKey[CharacteristicFilterPrefix.Length..];
                var parts = suffix.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    return CharacteristicFilterParseResult.Invalid(
                        $"Invalid filter parameter '{queryKey}'. Use format filter.<characteristicGuid>.min|max.");

                if (!Guid.TryParse(parts[0], out var characteristicId))
                    return CharacteristicFilterParseResult.Invalid(
                        $"Invalid characteristic GUID in filter parameter '{queryKey}'.");

                var bound = parts[1].ToLowerInvariant();
                if (bound is not ("min" or "max"))
                    return CharacteristicFilterParseResult.Invalid(
                        $"Invalid filter bound '{parts[1]}' in parameter '{queryKey}'. Use min or max.");

                if (!double.TryParse(queryPair.Value, out var boundValue))
                    return CharacteristicFilterParseResult.Invalid(
                        $"Invalid numeric value '{queryPair.Value}' in parameter '{queryKey}'.");

                if (!filters.TryGetValue(characteristicId, out var filter))
                {
                    filter = new CharacteristicFilter(characteristicId);
                    filters[characteristicId] = filter;
                }

                if (bound == "min")
                    filter.Min = boundValue;
                else
                    filter.Max = boundValue;
            }

            foreach (var filter in filters.Values)
            {
                if (filter.Min.HasValue && filter.Max.HasValue && filter.Min > filter.Max)
                {
                    return CharacteristicFilterParseResult.Invalid(
                        $"Invalid range for characteristic '{filter.Key}': min cannot be greater than max.");
                }
            }

            return CharacteristicFilterParseResult.Valid(filters.Values.ToList());
        }

        private sealed class CharacteristicFilter
        {
            public CharacteristicFilter(Guid key)
            {
                Key = key;
            }

            public Guid Key { get; }
            public double? Min { get; set; }
            public double? Max { get; set; }
        }

        private sealed class CharacteristicFilterParseResult
        {
            private CharacteristicFilterParseResult(List<CharacteristicFilter> filters, bool isValid, string? errorMessage)
            {
                Filters = filters;
                IsValid = isValid;
                ErrorMessage = errorMessage;
            }

            public List<CharacteristicFilter> Filters { get; }
            public bool IsValid { get; }
            public string? ErrorMessage { get; }

            public static CharacteristicFilterParseResult Valid(List<CharacteristicFilter> filters) =>
                new(filters, true, null);

            public static CharacteristicFilterParseResult Invalid(string errorMessage) =>
                new(new List<CharacteristicFilter>(), false, errorMessage);
        }

        /// <summary>
        /// Get single product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(Guid id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.CharacteristicValues)
                .ThenInclude(cv => cv.Characteristic)
                .Include(p => p.Images)
                .AsSplitQuery()
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

            if (request.CharacteristicValues.Any())
            {
                var requiredCharacteristics = await _context.CategoryCharacteristics
                    .Where(cc => cc.CategoryId == request.CategoryId)
                    .ToListAsync();

                foreach (var charValue in request.CharacteristicValues)
                {
                    if (requiredCharacteristics.Any(rc => rc.CharacteristicId == charValue.Key))
                    {
                        _context.ProductCharacteristicValues.Add(new ProductCharacteristicValue
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            CharacteristicId = charValue.Key,
                            Value = double.Parse(charValue.Value)
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            var createdProduct = await LoadProductForDto(product.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, MapToDto(createdProduct ?? product));
        }

        /// <summary>
        /// Update product (administrator only)
        /// </summary>
        [Authorize(Roles = "ADMINISTRATOR")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
        {
            var product = await _context.Products
                .Include(p => p.CharacteristicValues)
                .FirstOrDefaultAsync(p => p.Id == id);
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

            if (request.CharacteristicValues != null && request.CharacteristicValues.Any())
            {
                var existingCharValues = product.CharacteristicValues.ToDictionary(cv => cv.CharacteristicId);

                foreach (var charValue in request.CharacteristicValues)
                {
                    if (existingCharValues.TryGetValue(charValue.Key, out var existing))
                    {
                        existing.Value = double.Parse(charValue.Value);
                    }
                    else
                    {
                        _context.ProductCharacteristicValues.Add(new ProductCharacteristicValue
                        {
                            Id = Guid.NewGuid(),
                            ProductId = id,
                            CharacteristicId = charValue.Key,
                            Value = double.Parse(charValue.Value)
                        });
                    }
                }
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var updatedProduct = await LoadProductForDto(id);
            return Ok(MapToDto(updatedProduct ?? product));
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
                Characteristics = (product.CharacteristicValues ?? Enumerable.Empty<ProductCharacteristicValue>())
                    .Select(cv => new ProductCharacteristicValueDto
                    {
                        CharacteristicId = cv.CharacteristicId,
                        Name = cv.Characteristic?.Name ?? string.Empty,
                        Value = cv.Value,
                        Unit = cv.Characteristic?.Unit ?? string.Empty
                    }).ToList(),
                ImagePaths = (product.Images ?? Enumerable.Empty<ProductImage>())
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ObjectName)
                    .ToList()
            };
        }

        private Task<Product?> LoadProductForDto(Guid id)
        {
            return _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.CharacteristicValues)
                .ThenInclude(cv => cv.Characteristic)
                .Include(p => p.Images)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
