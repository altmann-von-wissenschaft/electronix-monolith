namespace Domain.Orders;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }  // Reference to Products module (no FK)
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}
