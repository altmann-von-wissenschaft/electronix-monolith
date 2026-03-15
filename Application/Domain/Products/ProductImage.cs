namespace Domain.Products;

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ObjectName { get; set; } = null!;  // Minio object path
    public int DisplayOrder { get; set; }
    public DateTime UploadedAt { get; set; }
}
