using System.ComponentModel.DataAnnotations;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.DTOs
{
    public class CreateLearningRequestDto
    {
        [Required] public string SenderId { get; set; } = string.Empty;
        [Required] public string ReceiverId { get; set; } = string.Empty;
        [Required] public int SkillId { get; set; }
    }

    public class UpdateRequestStatusDto
    {
        [Required] public RequestStatus Status { get; set; }
    }

    public class UpdateSessionDto
    {
        public DateTime? ScheduledTime { get; set; }
        public SessionStatus? Status { get; set; }
        public string? MeetingLink { get; set; }
    }

    public class CreateFeedbackDto
    {
        [Required] public int SessionId { get; set; }
        [Required] public string ReviewerId { get; set; } = string.Empty;
        [Required][Range(1, 5)] public int Rating { get; set; }
        [StringLength(500)] public string Comments { get; set; } = string.Empty;
    }
}