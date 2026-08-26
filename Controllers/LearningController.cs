using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.DTOs;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Controllers
{
    // Handles the full Learning Session & Feedback module:
    // requests -> sessions -> feedback
    [ApiController]
    [Route("api/[controller]")]
    public class LearningController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LearningController(ApplicationDbContext context) => _context = context;

        // ---------- Learning Requests ----------

        [HttpGet("requests/user/{userId}")]
        public async Task<IActionResult> GetRequestsByUser(string userId)
        {
            var requests = await _context.LearningRequests
                .Include(r => r.Sender).Include(r => r.Receiver).Include(r => r.Skill)
                .Where(r => r.SenderId == userId || r.ReceiverId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return Ok(requests);
        }

        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest(CreateLearningRequestDto dto)
        {
            if (dto.SenderId == dto.ReceiverId)
                return BadRequest(new { message = "Cannot send a request to yourself." });

            if (await _context.Skills.FindAsync(dto.SkillId) == null)
                return NotFound(new { message = "Skill not found." });

            var request = new LearningRequest
            {
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverId,
                SkillId = dto.SkillId,
                Status = RequestStatus.Pending
            };
            _context.LearningRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPut("requests/{id}/status")]
        public async Task<IActionResult> UpdateRequestStatus(int id, UpdateRequestStatusDto dto)
        {
            var request = await _context.LearningRequests.Include(r => r.Session)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound(new { message = "Request not found." });
            if (request.Status != RequestStatus.Pending)
                return BadRequest(new { message = "Only pending requests can be updated." });

            request.Status = dto.Status;

            if (dto.Status == RequestStatus.Accepted && request.Session == null)
            {
                _context.LearningSessions.Add(new LearningSession
                {
                    RequestId = request.Id,
                    ScheduledTime = DateTime.UtcNow.AddDays(1),
                    Status = SessionStatus.Scheduled
                });
            }

            await _context.SaveChangesAsync();
            return Ok(request);
        }

        // ---------- Learning Sessions ----------

        [HttpGet("sessions/user/{userId}")]
        public async Task<IActionResult> GetSessionsByUser(string userId)
        {
            var sessions = await _context.LearningSessions
                .Include(s => s.Request).ThenInclude(r => r!.Sender)
                .Include(s => s.Request).ThenInclude(r => r!.Receiver)
                .Where(s => s.Request!.SenderId == userId || s.Request!.ReceiverId == userId)
                .OrderBy(s => s.ScheduledTime)
                .ToListAsync();
            return Ok(sessions);
        }

        [HttpPut("sessions/{id}")]
        public async Task<IActionResult> UpdateSession(int id, UpdateSessionDto dto)
        {
            var session = await _context.LearningSessions.FindAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            if (dto.ScheduledTime.HasValue) session.ScheduledTime = dto.ScheduledTime.Value;
            if (dto.Status.HasValue) session.Status = dto.Status.Value;
            if (dto.MeetingLink != null) session.MeetingLink = dto.MeetingLink;

            await _context.SaveChangesAsync();
            return Ok(session);
        }

        // ---------- Feedback ----------

        [HttpGet("feedback/session/{sessionId}")]
        public async Task<IActionResult> GetFeedbackForSession(int sessionId)
        {
            var feedback = await _context.Feedbacks.Include(f => f.Reviewer)
                .Where(f => f.SessionId == sessionId).ToListAsync();
            return Ok(feedback);
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> AddFeedback(CreateFeedbackDto dto)
        {
            var session = await _context.LearningSessions.Include(s => s.Request)
                .FirstOrDefaultAsync(s => s.Id == dto.SessionId);
            if (session == null) return NotFound(new { message = "Session not found." });

            var isParticipant = session.Request != null &&
                (session.Request.SenderId == dto.ReviewerId || session.Request.ReceiverId == dto.ReviewerId);
            if (!isParticipant)
                return BadRequest(new { message = "Only session participants can leave feedback." });

            if (await _context.Feedbacks.AnyAsync(f => f.SessionId == dto.SessionId && f.ReviewerId == dto.ReviewerId))
                return BadRequest(new { message = "You already reviewed this session." });

            var feedback = new Feedback
            {
                SessionId = dto.SessionId,
                ReviewerId = dto.ReviewerId,
                Rating = dto.Rating,
                Comments = dto.Comments
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return Ok(feedback);
        }
    }
}