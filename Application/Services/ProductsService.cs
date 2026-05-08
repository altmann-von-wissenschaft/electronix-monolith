namespace Application.Services;

/// <summary>
/// HTTP client service for Products module communication
/// </summary>
public class ProductsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductsService> _logger;

    public ProductsService(HttpClient httpClient, IConfiguration configuration, ILogger<ProductsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetProductsUrl() => _configuration["ServiceUrls:Products"] ?? "http://localhost:80";

    /// <summary>
    /// Get product by ID
    /// </summary>
    public async Task<ProductResponse?> GetProductAsync(Guid productId)
    {
        try
        {
            var url = InterServiceUrl.Combine(GetProductsUrl(), "api/products", productId.ToString());
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<ProductResponse>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Products HTTP GET failed for product {ProductId}", productId);
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
    /// Update product stock via PUT <c>api/products/{id}</c> (matches monolith API). Intended for split deployments with forwarded authorization.
    /// </summary>
    public async Task<bool> UpdateStockAsync(Guid productId, int quantityChange)
    {
        try
        {
            var basePrefix = InterServiceUrl.Combine(GetProductsUrl(), "api/products", productId.ToString());
            var getResp = await _httpClient.GetAsync(basePrefix);
            if (!getResp.IsSuccessStatusCode)
                return false;

            var json = await getResp.Content.ReadAsStringAsync();
            var current = System.Text.Json.JsonSerializer.Deserialize<ProductResponse>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (current == null)
                return false;

            var newStock = current.Stock + quantityChange;
            if (newStock < 0)
                return false;

            var putBody = System.Text.Json.JsonSerializer.Serialize(new { stock = newStock });
            var content = new StringContent(putBody, System.Text.Encoding.UTF8, "application/json");
            var putUrl = InterServiceUrl.Combine(GetProductsUrl(), "api/products", productId.ToString());
            var response = await _httpClient.PutAsync(putUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Products HTTP stock update failed for product {ProductId}, delta {Delta}", productId, quantityChange);
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
