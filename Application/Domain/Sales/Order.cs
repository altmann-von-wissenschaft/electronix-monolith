using System;
using System.Collections.Generic;

namespace Domain.Sales
{
    public class Order
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Status { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid? ProcessedById { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}