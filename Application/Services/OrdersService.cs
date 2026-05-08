namespace Application.Services;

/// <summary>
/// HTTP client service for Orders module communication
/// </summary>
public class OrdersService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(HttpClient httpClient, IConfiguration configuration, ILogger<OrdersService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetOrdersUrl() => _configuration["ServiceUrls:Orders"] ?? "http://localhost:80";

    /// <summary>
    /// Create order from cart items (legacy HTTP integration — no in-repo callers; endpoint may not exist on monolith).
    /// </summary>
    public async Task<OrderResponse?> CreateOrderAsync(Guid userId, List<CartItemForOrder> items)
    {
        try
        {
            var request = new { userId, items };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var url = InterServiceUrl.Combine(GetOrdersUrl(), "api/orders", "internal");
            var response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<OrderResponse>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Orders HTTP POST internal failed for user {UserId}", userId);
            return null;
        }
    }
}

public class CartItemForOrder
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
}

public class SalesReportResponse
{
    public object Period { get; set; } = null!;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}
