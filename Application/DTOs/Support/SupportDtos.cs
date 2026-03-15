namespace Application.DTOs.Support;

public class QuestionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Subject { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsAnswered { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public Guid Id { get; set; }
    public Guid ManagerUserId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class CreateQuestionRequest
{
    public string Subject { get; set; } = null!;
    public string Content { get; set; } = null!;
}

public class CreateAnswerRequest
{
    public string Content { get; set; } = null!;
}
