using System.ComponentModel.DataAnnotations;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.DTOs
{
    public class CreateLearningRequestDto
    {
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
        [Required][Range(1, 5)] public int Rating { get; set; }
        [StringLength(500)] public string Comments { get; set; } = string.Empty;
    }

    public class PersonDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class LearningRequestDto
    {
        public int Id { get; set; }
        public PersonDto Sender { get; set; } = new();
        public PersonDto Receiver { get; set; } = new();
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? SessionId { get; set; }
    }

    public class LearningSessionDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public SessionStatus Status { get; set; }
        public string? MeetingLink { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public PersonDto Learner { get; set; } = new();
        public PersonDto Mentor { get; set; } = new();
    }

    public class FeedbackDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public PersonDto Reviewer { get; set; } = new();
        public int Rating { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
