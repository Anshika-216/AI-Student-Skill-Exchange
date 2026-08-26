using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIstudentskillexchange.Models
{
    public enum SessionStatus
    {
        Scheduled,
        Completed,
        Canceled
    }

    public class LearningSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestId { get; set; }
        [ForeignKey("RequestId")]
        public LearningRequest? Request { get; set; }

        [Required]
        public DateTime ScheduledTime { get; set; }

        [Required]
        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

        public string? MeetingLink { get; set; }

        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
