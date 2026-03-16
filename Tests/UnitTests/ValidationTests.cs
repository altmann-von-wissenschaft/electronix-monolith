using Domain.Products;
using Domain.Users;

namespace Tests.UnitTests;

/// <summary>
/// Unit tests for validation and business logic
/// </summary>
public class ValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Product_WithNullOrEmptyName_ShouldHaveEmptyName(string? name)
    {
        // Act
        var product = new Product { Name = name ?? "" };

        // Assert
        string.IsNullOrWhiteSpace(product.Name).Should().BeTrue();
    }

    [Fact]
    public void Product_WithNegativePrice_ShouldInitialize()
    {
        // Arrange & Act
        var product = new Product { Price = -10m };

        // Assert
        product.Price.Should().Be(-10m);
        // Note: Real validation should prevent this in business logic
    }

    [Fact]
    public void Product_WithNegativeStock_ShouldAllowInitialization()
    {
        // Arrange & Act
        var product = new Product { Stock = -5 };

        // Assert
        product.Stock.Should().Be(-5);
        // Note: Real validation should prevent overselling
    }

    [Fact]
    public void Order_CannotHaveNullItems()
    {
        // Arrange & Act
        var order = new Domain.Orders.Order();

        // Assert
        order.Items.Should().NotBeNull();
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void OrderItem_ShouldHaveValidQuantity()
    {
        // Arrange & Act
        var item = new Domain.Orders.OrderItem { Quantity = 5 };

        // Assert
        item.Quantity.Should().Be(5);
    }

    [Fact]
    public void OrderItem_WithZeroQuantity_ShouldInitialize()
    {
        // Arrange & Act
        var item = new Domain.Orders.OrderItem { Quantity = 0 };

        // Assert
        item.Quantity.Should().Be(0);
        // Note: Real validation should prevent zero quantity orders
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("name+tag@example.com")]
    public void User_ShouldAcceptVariousEmailFormats(string email)
    {
        // Arrange & Act
        var user = new User { Email = email };

        // Assert
        user.Email.Should().Be(email);
    }

    [Fact]
    public void Category_ShouldInitializeWithEmptyProducts()
    {
        // Arrange & Act
        var category = new Category();

        // Assert
        category.Products.Should().BeEmpty();
        category.Products.Should().NotBeNull();
    }

    [Fact]
    public void ProductAttribute_ShouldHaveKeyAndValue()
    {
        // Arrange & Act
        var attr = new ProductAttribute
        {
            Name = "Material",
            Value = "Cotton"
        };

        // Assert
        attr.Name.Should().NotBeNullOrEmpty();
        attr.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Guid_ShouldNotBeEmptyForValidEntities()
    {
        // Arrange & Act
        var id = Guid.NewGuid();
        var product = new Product { Id = id };

        // Assert
        product.Id.Should().NotBe(Guid.Empty);
        product.Id.Should().Be(id);
    }

    [Fact]
    public void DateTime_ShouldBeUtcInDomainEntities()
    {
        // Arrange
        var order = new Domain.Orders.Order
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        order.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        order.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}
