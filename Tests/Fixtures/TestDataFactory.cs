using System.Text.Json;

namespace Tests.Fixtures;

/// <summary>
/// Provides test data factories for creating test objects
/// </summary>
public class TestDataFactory
{
    public static readonly Guid ValidProductId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid ValidCategoryId = new("00000000-0000-0000-0000-000000000010");
    public static readonly Guid ValidUserId = new("9734c85d-20c5-47a1-8c97-6eda77a04735");

    public static class Products
    {
        public static JsonElement CreateProductRequest() =>
            JsonDocument.Parse(@"
            {
                ""name"": ""Test Product"",
                ""description"": ""A test product for unit tests"",
                ""price"": 99.99,
                ""stock"": 50,
                ""categoryId"": ""00000000-0000-0000-0000-000000000010""
            }").RootElement;

        public static JsonElement CreateProductWithNameRequest(string name) =>
            JsonDocument.Parse($@"
            {{
                ""name"": ""{name}"",
                ""description"": ""A test product for unit tests"",
                ""price"": 99.99,
                ""stock"": 50,
                ""categoryId"": ""00000000-0000-0000-0000-000000000010""
            }}").RootElement;
    }

    public static class Orders
    {
        public static JsonElement CreateOrderRequest() =>
            JsonDocument.Parse(@"
            {
                ""items"": [
                    {
                        ""productId"": ""00000000-0000-0000-0000-000000000001"",
                        ""quantity"": 2
                    }
                ]
            }").RootElement;
    }

    public static class Cart
    {
        public static JsonElement AddCartItemRequest(Guid productId, int quantity) =>
            JsonDocument.Parse($@"
            {{
                ""productId"": ""{productId}"",
                ""quantity"": {quantity}
            }}").RootElement;
    }

    public static class Auth
    {
        public static JsonElement LoginRequest(string email, string password) =>
            JsonDocument.Parse($@"
            {{
                ""email"": ""{email}"",
                ""password"": ""{password}""
            }}").RootElement;
    }
}
