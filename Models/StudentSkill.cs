using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIstudentskillexchange.Models
{
    public enum SkillType
    {
        ToTeach,
        ToLearn
    }

    public enum ProficiencyLevel
    {
        Beginner,
        Intermediate,
        Expert
    }

    public class StudentSkill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }

        [Required]
        public int SkillId { get; set; }
        [ForeignKey("SkillId")]
        public Skill? Skill { get; set; }

        [Required]
        public SkillType Type { get; set; }

        [Required]
        public ProficiencyLevel Level { get; set; }
    }
}
