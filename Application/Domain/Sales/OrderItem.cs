using System;

namespace Domain.Sales
{
    public class OrderItem
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public Guid ProductId { get; set; }

        public decimal PriceAtPurchase { get; set; }

        public int Quantity { get; set; }
    }
}