using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIstudentskillexchange.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }
        [ForeignKey("SessionId")]
        public LearningSession? Session { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;
        [ForeignKey("ReviewerId")]
        public ApplicationUser? Reviewer { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string Comments { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
