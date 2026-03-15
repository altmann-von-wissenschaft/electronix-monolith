namespace Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsHidden { get; set; }
    public string? MainImagePath { get; set; }
    public Guid CategoryId { get; set; }
    public List<ProductAttributeDto> Attributes { get; set; } = new();
    public List<string> ImagePaths { get; set; } = new();
}

public class ProductAttributeDto
{
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? Unit { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
    public List<ProductAttributeDto> Attributes { get; set; } = new();
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
    public Guid? CategoryId { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
}
