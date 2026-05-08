namespace Application.DTOs.Orders;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Staff user who last changed order status (from latest history row), if any.</summary>
    public Guid? LastStatusChangedByUserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}

public class CreateOrderRequest
{
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
}

public class SalesReportDto
{
    public SalesReportPeriodDto Period { get; set; } = null!;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    /// <summary>day = one point per calendar day; month = one point per calendar month</summary>
    public string Granularity { get; set; } = "day";
    public List<SalesReportPointDto> Series { get; set; } = new();
}

public class SalesReportPointDto
{
    /// <summary>UTC start of the bucket (day or first day of month)</summary>
    public DateTime PeriodStart { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class SalesReportPeriodDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
