using Application.DTOs.Cart;
using Application.Services;
using Domain.Cart;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Cart
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly CartDbContext _context;
        private readonly ProductsService _productsService;

        public CartController(CartDbContext context, ProductsService productsService)
        {
            _context = context;
            _productsService = productsService;
        }

        /// <summary>
        /// Get user's shopping cart
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
                return NotFound();

            return Ok(await MapToDtoAsync(cart));
        }

        /// <summary>
        /// Add item to cart
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                cart = new Domain.Cart.Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Call ProductsService to validate product and check stock
            var product = await _productsService.GetProductAsync(request.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            if (product.Stock < request.Quantity)
                return BadRequest(new { message = "Insufficient stock" });

            // Check if item already in cart
            var existingItem = cart.Items.FirstOrDefault(ci => ci.ProductId == request.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.AddedAt = DateTime.UtcNow;
            }
            else
            {
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    AddedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(await MapToDtoAsync(cart));
        }

        /// <summary>
        /// Update cart item quantity
        /// </summary>
        [HttpPut("items/{itemId}")]
        public async Task<IActionResult> UpdateCartItem(Guid itemId, [FromBody] UpdateCartItemRequest request)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId);

            if (cartItem == null)
                return NotFound();

            if (cartItem.Cart.UserId != userId.Value)
                return Forbid();

            if (request.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0" });

            // Call ProductsService to check stock availability
            var product = await _productsService.GetProductAsync(cartItem.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            if (product.Stock < request.Quantity)
                return BadRequest(new { message = "Insufficient stock" });

            cartItem.Quantity = request.Quantity;
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstAsync(c => c.Id == cartItem.CartId);

            return Ok(await MapToDtoAsync(cart));
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> RemoveFromCart(Guid itemId)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId);

            if (cartItem == null)
                return NotFound();

            if (cartItem.Cart.UserId != userId.Value)
                return Forbid();

            _context.CartItems.Remove(cartItem);
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstAsync(c => c.Id == cartItem.CartId);

            return Ok(await MapToDtoAsync(cart));
        }

        /// <summary>
        /// Clear entire cart
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
                return NotFound();

            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<CartDto> MapToDtoAsync(Domain.Cart.Cart cart)
        {
            var productIds = cart.Items.Select(ci => ci.ProductId).ToList();
            var products = new Dictionary<Guid, dynamic>();

            if (productIds.Any())
            {
                var productList = await _productsService.GetProductsAsync(productIds);
                if (productList != null)
                {
                    foreach (var product in productList)
                    {
                        products[product.Id] = product;
                    }
                }
            }

            var totalPrice = cart.Items.Sum(ci =>
            {
                if (products.TryGetValue(ci.ProductId, out var product))
                {
                    return product.Price * ci.Quantity;
                }
                return 0m;
            });

            return new CartDto
            {
                Id = cart.Id,
                Items = cart.Items.Select(ci =>
                {
                    var productName = "Unknown Product";
                    var productPrice = 0m;

                    if (products.TryGetValue(ci.ProductId, out var product))
                    {
                        productName = product.Name;
                        productPrice = product.Price;
                    }

                    return new CartItemDto
                    {
                        Id = ci.Id,
                        ProductId = ci.ProductId,
                        ProductName = productName,
                        ProductPrice = productPrice,
                        Quantity = ci.Quantity
                    };
                }).ToList(),
                TotalPrice = totalPrice
            };
        }
    }
}
