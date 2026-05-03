namespace Domain.Products;

public class Characteristic
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    
    public ICollection<CategoryCharacteristic> CategoryCharacteristics { get; set; } = new List<CategoryCharacteristic>();
    public ICollection<ProductCharacteristicValue> ProductValues { get; set; } = new List<ProductCharacteristicValue>();
}
