using Application.DTOs.Support;
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
        private readonly SupportDbContext _context;

        public SupportController(SupportDbContext context)
        {
            _context = context;
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
                .Where(q => q.UserId == userId.Value)
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            questions.ForEach(async q => await FillAnswerAsync(q));
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
                .Where(q => !q.IsAnswered)
                .OrderBy(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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

            var question = new Question
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Subject = request.Subject,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsAnswered = false
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

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
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            question.IsAnswered = true;
            _context.Answers.Add(answer);

            await _context.SaveChangesAsync();

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
