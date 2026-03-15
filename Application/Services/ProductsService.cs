namespace Application.Services;

/// <summary>
/// HTTP client service for Products module communication
/// </summary>
public class ProductsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ProductsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string GetProductsUrl() => _configuration["ServiceUrls:Products"] ?? "http://localhost:80";

    /// <summary>
    /// Get product by ID
    /// </summary>
    public async Task<ProductResponse?> GetProductAsync(Guid productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{GetProductsUrl()}/api/products/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<ProductResponse>(json, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get multiple products by IDs
    /// </summary>
    public async Task<List<ProductResponse>> GetProductsAsync(List<Guid> productIds)
    {
        var tasks = productIds.Select(id => GetProductAsync(id)).ToList();
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).Select(r => r!).ToList();
    }

    /// <summary>
    /// Update product stock
    /// </summary>
    public async Task<bool> UpdateStockAsync(Guid productId, int quantityChange)
    {
        try
        {
            var request = new { quantityChange };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync(
                $"{GetProductsUrl()}/api/products/{productId}/stock", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsHidden { get; set; }
}
