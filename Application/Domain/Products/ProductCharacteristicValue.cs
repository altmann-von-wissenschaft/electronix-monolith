namespace Domain.Products;

/// <summary>
/// Stores the actual value of a characteristic for a specific product.
/// Links a Product to a Characteristic with the product's specific value.
/// </summary>
public class ProductCharacteristicValue
{
    public Guid Id { get; set; }
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public Guid CharacteristicId { get; set; }
    public Characteristic Characteristic { get; set; } = null!;
    
    public double Value { get; set; }
}
