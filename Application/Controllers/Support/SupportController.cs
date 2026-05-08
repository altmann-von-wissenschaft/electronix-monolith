using Application.DTOs.Support;
using Application.Services;
using Domain.Support;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers.Support
{
    [ApiController]
    [Route("api/support")]
    [Authorize]
    public class SupportController : ControllerBase
    {
        private const int SubjectMin = 4;
        private const int SubjectMax = 120;
        private const int ContentMin = 10;
        private const int ContentMax = 2000;
        private const int AnswerMin = 8;
        private const int AnswerMax = 2000;
        private readonly SupportDbContext _context;
        private readonly IPushNotificationSender _push;

        public SupportController(SupportDbContext context, IPushNotificationSender push)
        {
            _context = context;
            _push = push;
        }

        /// <summary>
        /// Get user's questions
        /// </summary>
        [HttpGet("questions")]
        public async Task<IActionResult> GetMyQuestions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var questions = await _context.Questions
                .AsNoTracking()
                .Where(q => q.UserId == userId.Value)
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await FillAnswersAsync(questions);

            var dtos = questions.Select(MapToDto).ToList();
            return Ok(new { data = dtos, page, pageSize });
        }

        /// <summary>
        /// Get all unanswered questions (manager only)
        /// </summary>
        [Authorize(Roles = "MANAGER,ADMINISTRATOR")]
        [HttpGet("questions/unanswered")]
        public async Task<IActionResult> GetUnansweredQuestions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var questions = await _context.Questions
                .AsNoTracking()
                .Where(q => !q.IsAnswered)
                .OrderBy(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await FillAnswersAsync(questions);
            questions.ForEach(q => q.Answer = null);
            var dtos = questions.Select(MapToDto).ToList();
            return Ok(new { data = dtos, page, pageSize });
        }

        /// <summary>
        /// Get single question with answers
        /// </summary>
        [HttpGet("questions/{id}")]
        public async Task<IActionResult> GetQuestion(Guid id)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            await FillAnswerAsync(question);

            // Users can only see their own questions, managers can see all
            var userRoles = AuthToken.GetRoles(User).ToList();
            if (!userRoles.Contains("MANAGER") && !userRoles.Contains("ADMINISTRATOR") && question.UserId != userId.Value)
                return Forbid();

            return Ok(MapToDto(question));
        }

        /// <summary>
        /// Create a question
        /// </summary>
        [HttpPost("questions")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request)
        {
            var userId = AuthToken.GetID(User);
            if (!userId.HasValue)
                return Unauthorized();

            var subject = request.Subject?.Trim() ?? "";
            var content = request.Content?.Trim() ?? "";
            if (subject.Length < SubjectMin || subject.Length > SubjectMax)
                return BadRequest(new { message = $"Тема: от {SubjectMin} до {SubjectMax} символов." });
            if (content.Length < ContentMin || content.Length > ContentMax)
                return BadRequest(new { message = $"Текст обращения: от {ContentMin} до {ContentMax} символов." });

            var question = new Question
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Subject = subject,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsAnswered = false
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            await _push.NotifyNewSupportQuestionForStaffAsync(question.Id, question.Subject, HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, MapToDto(question));
        }

        /// <summary>
        /// Answer a question (manager only)
        /// </summary>
        [Authorize(Roles = "MANAGER,ADMINISTRATOR")]
        [HttpPost("questions/{questionId}/answer")]
        public async Task<IActionResult> AnswerQuestion(Guid questionId, [FromBody] CreateAnswerRequest request)
        {
            var managerId = AuthToken.GetID(User);
            if (!managerId.HasValue)
                return Unauthorized();

            var answerText = request.Content?.Trim() ?? "";
            if (answerText.Length < AnswerMin || answerText.Length > AnswerMax)
                return BadRequest(new { message = $"Ответ: от {AnswerMin} до {AnswerMax} символов." });

            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
                return NotFound();

            await FillAnswerAsync(question);
            if (question.Answer != null)
                return BadRequest(new { message = "Question already answered" });

            var answer = new Answer
            {
                Id = Guid.NewGuid(),
                QuestionId = questionId,
                ManagerUserId = managerId.Value,
                Content = answerText,
                CreatedAt = DateTime.UtcNow
            };

            question.IsAnswered = true;
            _context.Answers.Add(answer);

            await _context.SaveChangesAsync();

            await FillAnswerAsync(question);
            await _push.NotifySupportReplyAsync(question.UserId, question.Id, question.Subject, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetQuestion), new { id = questionId }, MapToDto(question));
        }

        /// <summary>
        /// Delete answer (manager who created it only)
        /// </summary>
        [Authorize(Roles = "MANAGER,ADMINISTRATOR")]
        [HttpDelete("questions/{questionId}/answers/{answerId}")]
        public async Task<IActionResult> DeleteAnswer(Guid questionId, Guid answerId)
        {
            var managerId = AuthToken.GetID(User);
            if (!managerId.HasValue)
                return Unauthorized();

            var answer = await _context.Answers.FindAsync(answerId);
            if (answer == null || answer.QuestionId != questionId)
                return NotFound();

            // Only the manager who created it or admin can delete
            if (answer.ManagerUserId != managerId.Value && !AuthToken.GetRoles(User).Contains("ADMINISTRATOR"))
                return Forbid();

            _context.Answers.Remove(answer);

            // Check if question still has answers
            var question = await _context.Questions
                .FirstAsync(q => q.Id == questionId);

            await FillAnswerAsync(question);

            if (question.Answer == null)
                question.IsAnswered = false;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Answer deleted" });
        }

        private async Task FillAnswerAsync(Question question)
        {
            question.Answer = await _context.Answers
                .Where(a => a.QuestionId == question.Id)
                .FirstOrDefaultAsync();
        }

        private async Task FillAnswersAsync(List<Question> questions)
        {
            if (questions.Count == 0) return;
            var ids = questions.Select(q => q.Id).ToList();
            var answers = await _context.Answers
                .Where(a => ids.Contains(a.QuestionId))
                .GroupBy(a => a.QuestionId)
                .Select(g => g.OrderByDescending(a => a.CreatedAt).First())
                .ToListAsync();
            var byQuestion = answers.ToDictionary(a => a.QuestionId, a => a);
            foreach (var q in questions)
            {
                q.Answer = byQuestion.GetValueOrDefault(q.Id);
            }
        }

        private QuestionDto MapToDto(Question question)
        {
            return new QuestionDto
            {
                Id = question.Id,
                UserId = question.UserId,
                Subject = question.Subject,
                Content = question.Content,
                IsAnswered = question.IsAnswered,
                CreatedAt = question.CreatedAt,
                Answer = question.Answer != null ? new AnswerDto
                {
                    Id = question.Answer.Id,
                    ManagerUserId = question.Answer.ManagerUserId,
                    Content = question.Answer.Content,
                    CreatedAt = question.Answer.CreatedAt
                } : null
            };
        }
    }
}
