namespace Domain.Reviews;

public class Review
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }  // Reference to Products module (no FK)
    // Product navigation removed - fetched via API call

    public Guid UserId { get; set; }  // Reference to Users module (no FK)
    public int Rating { get; set; }  // 1-5
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsApproved { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
