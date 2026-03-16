using System.Net.Http.Headers;
using System.Text.Json;

namespace Tests.Fixtures;

/// <summary>
/// Provides shared test context for integration tests
/// Handles HTTP client setup and test data management
/// </summary>
public class TestApplicationFixture : IAsyncLifetime
{
    private readonly string _baseUrl = "http://localhost:8080";
    private readonly string _adminEmail = "altmannvonw@icloud.com";
    private readonly string _adminPassword = "12345678";
    private readonly string _testUserEmail = "testuser@example.com";
    private readonly string _testUserPassword = "TestPassword123";

    public HttpClient HttpClient { get; private set; } = null!;
    public string? AdminToken { get; private set; }
    public string? TestUserToken { get; private set; }

    public async Task InitializeAsync()
    {
        HttpClient = new HttpClient();
        HttpClient.BaseAddress = new Uri(_baseUrl);
        HttpClient.Timeout = TimeSpan.FromSeconds(30);

        // Wait for application to be ready
        await WaitForApplicationReadyAsync();

        // Authenticate and get tokens
        AdminToken = await AuthenticateAsync(_adminEmail, _adminPassword);
        TestUserToken = await AuthenticateAsync(_testUserEmail, _testUserPassword);
    }

    public Task DisposeAsync()
    {
        HttpClient?.Dispose();
        return Task.CompletedTask;
    }

    private async Task WaitForApplicationReadyAsync(int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await HttpClient.GetAsync("/api/products");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch { }

            await Task.Delay(1000);
        }

        throw new InvalidOperationException("Application failed to start within timeout");
    }

    private async Task<string?> AuthenticateAsync(string email, string password)
    {
        var loginRequest = new { email, password };
        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        try
        {
            var response = await HttpClient.PostAsync("/api/users/login", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("token", out var tokenElement))
                {
                    return tokenElement.GetString();
                }
            }
        }
        catch { }

        return null;
    }

    public void SetAuthorizationHeader(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            HttpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public void ClearAuthorizationHeader()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }
}
