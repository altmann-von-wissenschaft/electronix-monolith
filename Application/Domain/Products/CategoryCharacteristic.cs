namespace Domain.Products;

/// <summary>
/// Join table connecting Categories to their Characteristics.
/// Defines which characteristics are applicable to products in a specific category.
/// For leaf categories only (categories without children).
/// </summary>
public class CategoryCharacteristic
{
    public Guid Id { get; set; }
    
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    public Guid CharacteristicId { get; set; }
    public Characteristic Characteristic { get; set; } = null!;
    
    public bool IsRequired { get; set; } = true;
}
