using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AIstudentskillexchange.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string? Bio { get; set; }

        // Navigation Properties for relationships
        public ICollection<StudentSkill> StudentSkills { get; set; } = new List<StudentSkill>();
        public ICollection<LearningRequest> SentRequests { get; set; } = new List<LearningRequest>();
        public ICollection<LearningRequest> ReceivedRequests { get; set; } = new List<LearningRequest>();
    }
}