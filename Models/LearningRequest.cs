using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIstudentskillexchange.Models
{
    public enum RequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class LearningRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;
        [ForeignKey("SenderId")]
        public ApplicationUser? Sender { get; set; }

        [Required]
        public string ReceiverId { get; set; } = string.Empty;
        [ForeignKey("ReceiverId")]
        public ApplicationUser? Receiver { get; set; }

        [Required]
        public int SkillId { get; set; }
        [ForeignKey("SkillId")]
        public Skill? Skill { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public LearningSession? Session { get; set; }
    }
}
