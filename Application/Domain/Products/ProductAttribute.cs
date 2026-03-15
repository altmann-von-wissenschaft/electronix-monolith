namespace Domain.Products;

public class ProductAttribute
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = null!;  // e.g., "Resistance", "Voltage"
    public string Value { get; set; } = null!;  // e.g., "10k", "5V"
    public string? Unit { get; set; }  // e.g., "Ohm", "V", "mF"
}
