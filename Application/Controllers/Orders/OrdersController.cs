using Application.DTOs.Orders;
using Application.Services;
using Domain.Orders;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Orders
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _ordersContext;
        private readonly CartDbContext _cartContext;
        private readonly ProductsService _productsService;

        public OrdersController(OrdersDbContext ordersContext, CartDbContext cartContext, ProductsService productsService)
        {
            _ordersContext = ordersContext;
            _cartContext = cartContext;
            _productsService = productsService;
        }

        /// <summary>
        /// Get user's orders
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var orders = await _ordersContext.Orders
                .Where(o => o.UserId == userId.Value)
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var dtos = orders.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Get single order
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var order = await _ordersContext.Orders
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // Clients can only see their own orders
            var userRoles = AuthToken.GetRoles(User).ToList();
            if (!userRoles.Contains("MANAGER") && !userRoles.Contains("ADMINISTRATOR") && order.UserId != userId.Value)
                return Forbid();

            return Ok(MapToDto(order));
        }

        /// <summary>
        /// Create new order from cart (client)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder()
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var cart = await _cartContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.Items.Any())
                return BadRequest(new { message = "Cart is empty" });

            // Get all product details from ProductsService
            var productIds = cart.Items.Select(ci => ci.ProductId).ToList();
            var products = await _productsService.GetProductsAsync(productIds);
            
            if (products == null || !products.Any())
                return BadRequest(new { message = "Products not found" });

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalAmount = 0
            };

            foreach (var cartItem in cart.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (product == null || cartItem.Quantity > product.Stock)
                    return BadRequest(new { message = $"Product {product?.Name} is not available in requested quantity" });

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = cartItem.Quantity,
                    PriceAtPurchase = product.Price
                };

                order.Items.Add(orderItem);
                order.TotalAmount += product.Price * cartItem.Quantity;
            }

            // Create initial status history
            var statusHistory = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = OrderStatus.Pending,
                ChangedAt = DateTime.UtcNow
            };
            order.StatusHistory.Add(statusHistory);

            _ordersContext.Orders.Add(order);
            await _ordersContext.SaveChangesAsync();

            // Update product stock via ProductsService
            foreach (var cartItem in cart.Items)
            {
                await _productsService.UpdateStockAsync(cartItem.ProductId, -cartItem.Quantity);
            }

            // Clear cart
            _cartContext.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            await _cartContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, MapToDto(order));
        }

        /// <summary>
        /// Cancel order (client can cancel pending orders only)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var order = await _ordersContext.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.UserId != userId.Value)
                return Forbid();

            if (order.Status != OrderStatus.Pending)
                return BadRequest(new { message = "Can only cancel pending orders" });

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            // Restore product stock via ProductsService
            foreach (var item in order.Items)
            {
                await _productsService.UpdateStockAsync(item.ProductId, item.Quantity);
            }

            var statusHistory = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = OrderStatus.Cancelled,
                ChangedByUserId = userId.Value,
                ChangedAt = DateTime.UtcNow
            };
            _ordersContext.OrderStatusHistories.Add(statusHistory);

            await _ordersContext.SaveChangesAsync();
            return Ok(MapToDto(order));
        }

        /// <summary>
        /// Update order status (manager/administrator only)
        /// </summary>
        [Authorize(Roles = "MANAGER,ADMINISTRATOR")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            var managerId = AuthToken.GetID(User);
            if (!managerId.HasValue)
                return Unauthorized();

            var order = await _ordersContext.Orders
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
                return BadRequest(new { message = "Invalid order status" });

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            var statusHistory = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = newStatus,
                ChangedByUserId = managerId.Value,
                ChangedAt = DateTime.UtcNow,
                Notes = request.Notes
            };
            _ordersContext.OrderStatusHistories.Add(statusHistory);

            await _ordersContext.SaveChangesAsync();
            return Ok(MapToDto(order));
        }

        /// <summary>
        /// Get all orders (manager/administrator only)
        /// </summary>
        [Authorize(Roles = "MANAGER,ADMINISTRATOR")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllOrders([FromQuery] string? status = null)
        {
            var query = _ordersContext.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
                    return BadRequest(new { message = "Invalid status filter" });
                query = query.Where(o => o.Status == statusEnum);
            }

            var orders = await query
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var dtos = orders.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Get sales report for completed orders (manager/administrator only)
        /// </summary>
        [Authorize(Roles = "MANAGER,ADMINISTRATOR")]
        [HttpGet("reports/sales")]
        public async Task<IActionResult> GetSalesReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            // Ensure DateTime values are in UTC
            var start = startDate.HasValue 
                ? (startDate.Value.Kind == DateTimeKind.Utc ? startDate.Value : startDate.Value.ToUniversalTime())
                : DateTime.UtcNow.AddMonths(-1);
            
            var end = endDate.HasValue 
                ? (endDate.Value.Kind == DateTimeKind.Utc ? endDate.Value : endDate.Value.ToUniversalTime())
                : DateTime.UtcNow;

            var completedOrders = await _ordersContext.Orders
                .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= start && o.CreatedAt <= end)
                .ToListAsync();

            var report = new SalesReportDto
            {
                Period = new SalesReportPeriodDto
                {
                    StartDate = start,
                    EndDate = end
                },
                TotalOrders = completedOrders.Count,
                TotalRevenue = completedOrders.Sum(o => o.TotalAmount),
                AverageOrderValue = completedOrders.Any() ? completedOrders.Sum(o => o.TotalAmount) / completedOrders.Count : 0
            };

            return Ok(report);
        }

        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.Items.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList()
            };
        }
    }
}
