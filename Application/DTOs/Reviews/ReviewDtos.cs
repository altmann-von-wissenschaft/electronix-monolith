namespace Application.DTOs.Reviews;

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}
