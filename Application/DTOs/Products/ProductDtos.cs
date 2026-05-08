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
    public List<ProductCharacteristicValueDto> Characteristics { get; set; } = new();
    public List<string> ImagePaths { get; set; } = new();
}

public class ProductCharacteristicValueDto
{
    public Guid CharacteristicId { get; set; }
    public string Name { get; set; } = null!;
    public double Value { get; set; }
    public string Unit { get; set; } = null!;
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
    public Dictionary<Guid, string> CharacteristicValues { get; set; } = new();
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
    public Guid? CategoryId { get; set; }
    public Dictionary<Guid, string>? CharacteristicValues { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public List<CategoryCharacteristicDto> Characteristics { get; set; } = new();
}

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public List<AssignCharacteristicRequest>? Characteristics { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public Guid? ParentId { get; set; }
    public int? DisplayOrder { get; set; }
    public List<AssignCharacteristicRequest>? Characteristics { get; set; }
}

public class CharacteristicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
}

public class CreateCharacteristicRequest
{
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
}

public class CategoryCharacteristicDto
{
    public Guid Id { get; set; }
    public Guid CharacteristicId { get; set; }
    public string CharacteristicName { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public bool IsRequired { get; set; }
}

public class AssignCharacteristicRequest
{
    public Guid CharacteristicId { get; set; }
    public bool IsRequired { get; set; } = true;
}

public class UpdateCharacteristicAssignmentRequest
{
    public bool IsRequired { get; set; }
}
