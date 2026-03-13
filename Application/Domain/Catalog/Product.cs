using System;
using System.Collections.Generic;

namespace Domain.Catalog
{
    public class Product
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int QuantityAvailable { get; set; }

        public bool IsHidden { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<ProductAttributeValue> AttributeValues { get; set; }
            = new List<ProductAttributeValue>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}