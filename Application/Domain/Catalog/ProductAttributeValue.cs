using System;

namespace Domain.Catalog
{
    public class ProductAttributeValue
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid AttributeId { get; set; }
        public ProductAttribute Attribute { get; set; } = null!;

        public string Value { get; set; } = null!; // 10k, 100, 16 и т.д.
    }
}