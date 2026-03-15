namespace Domain.Products;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsHidden { get; set; } = false;
    public string? MainImagePath { get; set; }  // Minio object name
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    // CartItems, OrderItems, Reviews removed - fetched via API calls
}
