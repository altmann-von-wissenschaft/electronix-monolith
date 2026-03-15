namespace Domain.Orders;

public class OrderStatusHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public OrderStatus Status { get; set; }
    public Guid? ChangedByUserId { get; set; }  // Manager/Admin who changed the status
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
}
