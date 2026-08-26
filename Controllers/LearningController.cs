using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.DTOs;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LearningController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LearningController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static PersonDto ToPerson(string id, ApplicationUser? user) => new()
        {
            Id = id,
            FullName = user?.FullName ?? string.Empty
        };

        private static LearningRequestDto ToDto(LearningRequest r) => new()
        {
            Id = r.Id,
            Sender = ToPerson(r.SenderId, r.Sender),
            Receiver = ToPerson(r.ReceiverId, r.Receiver),
            SkillId = r.SkillId,
            SkillName = r.Skill?.Name ?? string.Empty,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            SessionId = r.Session?.Id
        };

        private static LearningSessionDto ToDto(LearningSession s) => new()
        {
            Id = s.Id,
            RequestId = s.RequestId,
            ScheduledTime = s.ScheduledTime,
            Status = s.Status,
            MeetingLink = s.MeetingLink,
            SkillName = s.Request?.Skill?.Name ?? string.Empty,
            Learner = ToPerson(s.Request?.SenderId ?? string.Empty, s.Request?.Sender),
            Mentor = ToPerson(s.Request?.ReceiverId ?? string.Empty, s.Request?.Receiver)
        };

        private static FeedbackDto ToDto(Feedback f) => new()
        {
            Id = f.Id,
            SessionId = f.SessionId,
            Reviewer = ToPerson(f.ReviewerId, f.Reviewer),
            Rating = f.Rating,
            Comments = f.Comments,
            CreatedAt = f.CreatedAt
        };

        [HttpGet("requests")]
        public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User)!;

            var requests = await _context.LearningRequests
                .AsNoTracking()
                .Include(r => r.Sender).Include(r => r.Receiver)
                .Include(r => r.Skill).Include(r => r.Session)
                .Where(r => r.SenderId == userId || r.ReceiverId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            return Ok(requests.Select(ToDto));
        }

        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest(
            CreateLearningRequestDto dto, CancellationToken cancellationToken)
        {
            var senderId = _userManager.GetUserId(User)!;

            if (senderId == dto.ReceiverId)
                return BadRequest(new { message = "Cannot send a request to yourself." });

            var receiver = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.ReceiverId, cancellationToken);
            if (receiver == null)
                return NotFound(new { message = "Receiver not found." });

            var skill = await _context.Skills.FindAsync([dto.SkillId], cancellationToken);
            if (skill == null)
                return NotFound(new { message = "Skill not found." });

            var duplicate = await _context.LearningRequests.AnyAsync(
                r => r.SenderId == senderId
                     && r.ReceiverId == dto.ReceiverId
                     && r.SkillId == dto.SkillId
                     && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Accepted),
                cancellationToken);

            if (duplicate)
                return Conflict(new { message = "You already have an open request to this peer for that skill." });

            var request = new LearningRequest
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                SkillId = dto.SkillId,
                Status = RequestStatus.Pending
            };

            _context.LearningRequests.Add(request);
            await _context.SaveChangesAsync(cancellationToken);

            var sender = await _userManager.GetUserAsync(User);

            return Ok(new LearningRequestDto
            {
                Id = request.Id,
                Sender = ToPerson(senderId, sender),
                Receiver = ToPerson(receiver.Id, receiver),
                SkillId = skill.Id,
                SkillName = skill.Name,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                SessionId = null
            });
        }

        [HttpPut("requests/{id}/status")]
        public async Task<IActionResult> UpdateRequestStatus(
            int id, UpdateRequestStatusDto dto, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User)!;

            var request = await _context.LearningRequests
                .Include(r => r.Sender).Include(r => r.Receiver)
                .Include(r => r.Skill).Include(r => r.Session)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (request == null)
                return NotFound(new { message = "Request not found." });

            if (request.ReceiverId != userId)
                return Forbid();

            if (request.Status != RequestStatus.Pending)
                return BadRequest(new { message = "Only pending requests can be updated." });

            if (dto.Status == RequestStatus.Pending)
                return BadRequest(new { message = "A request can only be moved to Accepted or Rejected." });

            request.Status = dto.Status;

            if (dto.Status == RequestStatus.Accepted && request.Session == null)
            {
                var session = new LearningSession
                {
                    RequestId = request.Id,
                    ScheduledTime = DateTime.UtcNow.AddDays(1),
                    Status = SessionStatus.Scheduled
                };
                _context.LearningSessions.Add(session);
                request.Session = session;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToDto(request));
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetMySessions(CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User)!;

            var sessions = await _context.LearningSessions
                .AsNoTracking()
                .Include(s => s.Request).ThenInclude(r => r!.Sender)
                .Include(s => s.Request).ThenInclude(r => r!.Receiver)
                .Include(s => s.Request).ThenInclude(r => r!.Skill)
                .Where(s => s.Request!.SenderId == userId || s.Request!.ReceiverId == userId)
                .OrderBy(s => s.ScheduledTime)
                .ToListAsync(cancellationToken);

            return Ok(sessions.Select(ToDto));
        }

        [HttpPut("sessions/{id}")]
        public async Task<IActionResult> UpdateSession(
            int id, UpdateSessionDto dto, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User)!;

            var session = await _context.LearningSessions
                .Include(s => s.Request).ThenInclude(r => r!.Sender)
                .Include(s => s.Request).ThenInclude(r => r!.Receiver)
                .Include(s => s.Request).ThenInclude(r => r!.Skill)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (session == null)
                return NotFound(new { message = "Session not found." });

            if (session.Request == null
                || (session.Request.SenderId != userId && session.Request.ReceiverId != userId))
                return Forbid();

            if (session.Status != SessionStatus.Scheduled)
                return BadRequest(new { message = "Only a scheduled session can be changed." });

            if (dto.ScheduledTime.HasValue)
            {
                if (dto.ScheduledTime.Value < DateTime.UtcNow)
                    return BadRequest(new { message = "A session cannot be scheduled in the past." });

                session.ScheduledTime = dto.ScheduledTime.Value;
            }

            if (dto.Status.HasValue) session.Status = dto.Status.Value;
            if (dto.MeetingLink != null) session.MeetingLink = dto.MeetingLink;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToDto(session));
        }

        [HttpGet("feedback/session/{sessionId}")]
        public async Task<IActionResult> GetFeedbackForSession(
            int sessionId, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User)!;

            var session = await _context.LearningSessions
                .AsNoTracking()
                .Include(s => s.Request)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
                return NotFound(new { message = "Session not found." });

            if (session.Request == null
                || (session.Request.SenderId != userId && session.Request.ReceiverId != userId))
                return Forbid();

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .Include(f => f.Reviewer)
                .Where(f => f.SessionId == sessionId)
                .ToListAsync(cancellationToken);

            return Ok(feedback.Select(ToDto));
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> AddFeedback(
            CreateFeedbackDto dto, CancellationToken cancellationToken)
        {
            var reviewerId = _userManager.GetUserId(User)!;

            var session = await _context.LearningSessions
                .Include(s => s.Request)
                .FirstOrDefaultAsync(s => s.Id == dto.SessionId, cancellationToken);

            if (session == null)
                return NotFound(new { message = "Session not found." });

            var isParticipant = session.Request != null
                && (session.Request.SenderId == reviewerId || session.Request.ReceiverId == reviewerId);

            if (!isParticipant)
                return Forbid();

            if (session.Status != SessionStatus.Completed)
                return BadRequest(new { message = "Feedback can only be left on a completed session." });

            if (await _context.Feedbacks.AnyAsync(
                    f => f.SessionId == dto.SessionId && f.ReviewerId == reviewerId, cancellationToken))
                return Conflict(new { message = "You already reviewed this session." });

            var feedback = new Feedback
            {
                SessionId = dto.SessionId,
                ReviewerId = reviewerId,
                Rating = dto.Rating,
                Comments = dto.Comments
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync(cancellationToken);

            var reviewer = await _userManager.GetUserAsync(User);

            return Ok(new FeedbackDto
            {
                Id = feedback.Id,
                SessionId = feedback.SessionId,
                Reviewer = ToPerson(reviewerId, reviewer),
                Rating = feedback.Rating,
                Comments = feedback.Comments,
                CreatedAt = feedback.CreatedAt
            });
        }
    }
}
