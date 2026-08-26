using System.ComponentModel.DataAnnotations;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.DTOs
{
    public class CreateSkillDto
    {
        [Required]
        public int SkillId { get; set; }

        public SkillType Type { get; set; } = SkillType.ToTeach;
        public ProficiencyLevel Level { get; set; } = ProficiencyLevel.Beginner;
    }

    public class UpdateSkillDto
    {
        public SkillType? Type { get; set; }
        public ProficiencyLevel? Level { get; set; }
    }

    public class StudentSkillDto
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public SkillType Type { get; set; }
        public ProficiencyLevel Level { get; set; }
    }

    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class ProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
    }
}
