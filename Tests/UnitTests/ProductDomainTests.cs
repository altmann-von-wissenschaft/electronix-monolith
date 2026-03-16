using Domain.Products;

namespace Tests.UnitTests;

/// <summary>
/// Unit tests for Product domain models
/// </summary>
public class ProductDomainTests
{
    [Fact]
    public void Product_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.Id.Should().Be(Guid.Empty);
        product.Name.Should().BeNullOrEmpty();
        product.Price.Should().Be(0);
        product.Stock.Should().Be(0);
        product.Attributes.Should().BeEmpty();
        product.Images.Should().BeEmpty();
    }

    [Fact]
    public void Product_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "A test product",
            Price = 99.99m,
            Stock = 50,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        product.Id.Should().Be(productId);
        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("A test product");
        product.Price.Should().Be(99.99m);
        product.Stock.Should().Be(50);
        product.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public void Product_ShouldAddAttributes()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Name = "Test" };
        var attribute = new ProductAttribute
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Name = "Color",
            Value = "Red"
        };

        // Act
        product.Attributes.Add(attribute);

        // Assert
        product.Attributes.Should().HaveCount(1);
        product.Attributes.First().Name.Should().Be("Color");
        product.Attributes.First().Value.Should().Be("Red");
    }

    [Fact]
    public void Product_ShouldAddImages()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Name = "Test" };
        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ObjectName = "test/image.jpg",
            UploadedAt = DateTime.UtcNow
        };

        // Act
        product.Images.Add(image);

        // Assert
        product.Images.Should().HaveCount(1);
        product.Images.First().ObjectName.Should().Be("test/image.jpg");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Product_ShouldAcceptVariousStockLevels(int stock)
    {
        // Arrange & Act
        var product = new Product { Stock = stock };

        // Assert
        product.Stock.Should().Be(stock);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(99.99)]
    [InlineData(1000.00)]
    public void Product_ShouldAcceptVariousPrices(decimal price)
    {
        // Arrange & Act
        var product = new Product { Price = price };

        // Assert
        product.Price.Should().Be(price);
    }

    [Fact]
    public void ProductAttribute_ShouldStoreKeyValuePairs()
    {
        // Arrange & Act
        var attribute = new ProductAttribute
        {
            Id = Guid.NewGuid(),
            Name = "Size",
            Value = "Large"
        };

        // Assert
        attribute.Name.Should().Be("Size");
        attribute.Value.Should().Be("Large");
    }

    [Fact]
    public void ProductImage_ShouldStoreImageMetadata()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var image = new ProductImage
        {
            Id = imageId,
            ProductId = productId,
            ObjectName = "products/123/image-uuid.jpg",
            UploadedAt = createdAt
        };

        // Assert
        image.Id.Should().Be(imageId);
        image.ProductId.Should().Be(productId);
        image.ObjectName.Should().Be("products/123/image-uuid.jpg");
        image.UploadedAt.Should().Be(createdAt);
    }
}
