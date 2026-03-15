namespace Domain.Cart;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public Guid ProductId { get; set; }  // Reference to Products module (no FK)
    // Product navigation removed - fetched via API call

    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }
}
