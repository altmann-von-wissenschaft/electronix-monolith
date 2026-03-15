namespace Domain.Support;

public class Question
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Subject { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsAnswered { get; set; } = false;

    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
