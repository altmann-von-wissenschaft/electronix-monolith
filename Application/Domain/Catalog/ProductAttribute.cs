using System;
using System.Collections.Generic;

namespace Domain.Catalog
{
    public class ProductAttribute
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;   // Сопротивление
        public string? Unit { get; set; }           // Ом, В, мкФ

        public ICollection<ProductAttributeValue> Values { get; set; }
            = new List<ProductAttributeValue>();
    }
}