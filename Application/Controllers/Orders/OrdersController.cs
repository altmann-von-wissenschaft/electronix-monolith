using Application.DTOs.Orders;
using Application.Services;
using Domain.Orders;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Controllers.Orders
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _ordersContext;
        private readonly CartDbContext _cartContext;
        private readonly ProductsDbContext _productsContext;
        private readonly ProductsService _productsService;
        private readonly IPushNotificationSender _push;

        public OrdersController(
            OrdersDbContext ordersContext,
            CartDbContext cartContext,
            ProductsDbContext productsContext,
            ProductsService productsService,
            IPushNotificationSender push)
        {
            _ordersContext = ordersContext;
            _cartContext = cartContext;
            _productsContext = productsContext;
            _productsService = productsService;
            _push = push;
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
                .AsNoTracking()
                .Where(o => o.UserId == userId.Value)
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .AsSplitQuery()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var dtos = await MapOrdersToDtosAsync(orders);
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
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // Clients can only see their own orders
            var userRoles = AuthToken.GetRoles(User).ToList();
            if (!userRoles.Contains("MANAGER") && !userRoles.Contains("ADMINISTRATOR") && order.UserId != userId.Value)
                return Forbid();

            var names = await ResolveProductNames(order.Items.Select(i => i.ProductId));
            return Ok(MapToDtoWithNames(order, names));
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

            await using var transaction = await _ordersContext.Database.BeginTransactionAsync();
            _productsContext.Database.UseTransaction(transaction.GetDbTransaction());
            _cartContext.Database.UseTransaction(transaction.GetDbTransaction());

            try
            {
                _ordersContext.Orders.Add(order);
                await _ordersContext.SaveChangesAsync();

                foreach (var cartItem in cart.Items)
                {
                    var productRow = await _productsContext.Products.FirstAsync(p => p.Id == cartItem.ProductId);
                    productRow.Stock -= cartItem.Quantity;
                    productRow.UpdatedAt = DateTime.UtcNow;
                }

                await _productsContext.SaveChangesAsync();

                _cartContext.CartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = DateTime.UtcNow;
                await _cartContext.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var names = await ResolveProductNames(order.Items.Select(i => i.ProductId));
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, MapToDtoWithNames(order, names));
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

            await using var transaction = await _ordersContext.Database.BeginTransactionAsync();
            _productsContext.Database.UseTransaction(transaction.GetDbTransaction());

            try
            {
                foreach (var item in order.Items)
                {
                    var productRow = await _productsContext.Products.FirstAsync(p => p.Id == item.ProductId);
                    productRow.Stock += item.Quantity;
                    productRow.UpdatedAt = DateTime.UtcNow;
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

                await _productsContext.SaveChangesAsync();
                await _ordersContext.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            order = await _ordersContext.Orders
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .AsSplitQuery()
                .FirstAsync(o => o.Id == id);
            var namesCancel = await ResolveProductNames(order.Items.Select(i => i.ProductId));
            return Ok(MapToDtoWithNames(order, namesCancel));
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
                .Include(o => o.Items)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
                return BadRequest(new { message = "Invalid order status" });

            if (order.Status == OrderStatus.Cancelled)
                return BadRequest(new { message = "Cannot change a cancelled order" });

            if (newStatus == OrderStatus.Cancelled)
            {
                var noteLen = request.Notes?.Trim().Length ?? 0;
                if (noteLen <= 20)
                    return BadRequest(new { message = "Cancellation requires a note longer than 20 characters" });
            }
            else
            {
                static int PipelineRank(OrderStatus s) => s switch
                {
                    OrderStatus.Pending => 0,
                    OrderStatus.Processing => 1,
                    OrderStatus.ReadyForPickup => 2,
                    OrderStatus.Completed => 3,
                    OrderStatus.Cancelled => -1,
                    _ => -1
                };

                var currentRank = PipelineRank(order.Status);
                var nextRank = PipelineRank(newStatus);
                if (nextRank <= currentRank)
                    return BadRequest(new { message = "Invalid status transition (cannot revert to an earlier stage)" });
            }

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
            await _push.NotifyOrderStatusChangedAsync(order.UserId, order.Id, newStatus.ToString(), HttpContext.RequestAborted);
            var namesStatus = await ResolveProductNames(order.Items.Select(i => i.ProductId));
            return Ok(MapToDtoWithNames(order, namesStatus));
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
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .AsSplitQuery()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var dtos = await MapOrdersToDtosAsync(orders);
            return Ok(dtos);
        }

        /// <summary>
        /// Sales report: revenue from orders marked Completed in the period (by status history timestamp). Manager/admin only.
        /// </summary>
        [Authorize(Roles = "MANAGER,ADMINISTRATOR")]
        [HttpGet("reports/sales")]
        public async Task<IActionResult> GetSalesReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var endDayEnd = endDate.HasValue
                ? UtcEndOfDay(NormalizeToUtc(endDate.Value))
                : UtcEndOfDay(DateTime.UtcNow);

            var startDayStart = startDate.HasValue
                ? UtcStartOfDay(NormalizeToUtc(startDate.Value))
                : UtcStartOfDay(UtcStartOfDay(endDayEnd).AddDays(-30));

            if (endDayEnd < startDayStart)
                return BadRequest(new { message = "Конец периода не может быть раньше начала" });

            var histories = await _ordersContext.OrderStatusHistories
                .AsNoTracking()
                .Where(h => h.Status == OrderStatus.Completed
                    && h.ChangedAt >= startDayStart
                    && h.ChangedAt <= endDayEnd)
                .Include(h => h.Order)
                .ToListAsync();

            var rows = histories
                .Select(h => new { h.OrderId, h.ChangedAt, Revenue = h.Order.TotalAmount })
                .ToList();

            var distinctOrders = rows
                .GroupBy(r => r.OrderId)
                .Select(g => g.OrderByDescending(x => x.ChangedAt).First())
                .ToList();

            var totalOrders = distinctOrders.Count;
            var totalRevenue = distinctOrders.Sum(x => x.Revenue);
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

            var spanDays = (UtcStartOfDay(endDayEnd) - UtcStartOfDay(startDayStart)).TotalDays + 1;
            var useDailyBuckets = spanDays <= 62;
            var granularity = useDailyBuckets ? "day" : "month";

            var bucketTotals = useDailyBuckets
                ? rows
                    .GroupBy(r => UtcStartOfDay(r.ChangedAt))
                    .ToDictionary(
                        g => g.Key,
                        g => (
                            Revenue: g.Sum(x => x.Revenue),
                            OrderCount: g.Select(x => x.OrderId).Distinct().Count()))
                : rows
                    .GroupBy(r => MonthStartUtc(r.ChangedAt))
                    .ToDictionary(
                        g => g.Key,
                        g => (
                            Revenue: g.Sum(x => x.Revenue),
                            OrderCount: g.Select(x => x.OrderId).Distinct().Count()));

            var series = new List<SalesReportPointDto>();
            if (useDailyBuckets)
            {
                for (var d = UtcStartOfDay(startDayStart); d <= UtcStartOfDay(endDayEnd); d = d.AddDays(1))
                {
                    bucketTotals.TryGetValue(d, out var agg);
                    series.Add(new SalesReportPointDto
                    {
                        PeriodStart = d,
                        Revenue = agg.Revenue,
                        OrderCount = agg.OrderCount,
                    });
                }
            }
            else
            {
                var cursor = MonthStartUtc(startDayStart);
                var last = MonthStartUtc(endDayEnd);
                while (cursor <= last)
                {
                    bucketTotals.TryGetValue(cursor, out var agg);
                    series.Add(new SalesReportPointDto
                    {
                        PeriodStart = cursor,
                        Revenue = agg.Revenue,
                        OrderCount = agg.OrderCount,
                    });
                    cursor = cursor.AddMonths(1);
                }
            }

            var report = new SalesReportDto
            {
                Period = new SalesReportPeriodDto
                {
                    StartDate = startDayStart,
                    EndDate = endDayEnd,
                },
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                Granularity = granularity,
                Series = series,
            };

            return Ok(report);
        }

        private static DateTime NormalizeToUtc(DateTime dt) =>
            dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            };

        private static DateTime UtcStartOfDay(DateTime utcAny)
        {
            var utc = NormalizeToUtc(utcAny);
            return new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static DateTime UtcEndOfDay(DateTime utcAny)
        {
            return UtcStartOfDay(utcAny).AddDays(1).AddTicks(-1);
        }

        private static DateTime MonthStartUtc(DateTime utcAny)
        {
            var utc = NormalizeToUtc(utcAny);
            return new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        private async Task<Dictionary<Guid, string>> ResolveProductNames(IEnumerable<Guid> productIds)
        {
            var idList = productIds.Distinct().ToList();
            if (idList.Count == 0)
                return new Dictionary<Guid, string>();

            var products = await _productsService.GetProductsAsync(idList);
            if (products == null || products.Count == 0)
                return new Dictionary<Guid, string>();

            return products.ToDictionary(p => p.Id, p => p.Name);
        }

        private async Task<List<OrderDto>> MapOrdersToDtosAsync(List<Order> orders)
        {
            var allIds = orders.SelectMany(o => o.Items ?? Array.Empty<OrderItem>()).Select(i => i.ProductId);
            var names = await ResolveProductNames(allIds);
            return orders.Select(o => MapToDtoWithNames(o, names)).ToList();
        }

        private static OrderDto MapToDtoWithNames(Order order, Dictionary<Guid, string> names)
        {
            var items = order.Items ?? Array.Empty<OrderItem>();
            var latestHistory = order.StatusHistory?
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault();
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                LastStatusChangedByUserId = latestHistory?.ChangedByUserId,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = items.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = names.GetValueOrDefault(oi.ProductId),
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList()
            };
        }
    }
}
