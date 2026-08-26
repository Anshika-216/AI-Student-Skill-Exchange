using System.ComponentModel.DataAnnotations;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.DTOs
{
    public class CreateSkillDto
    {
        [Required]
        public int SkillId { get; set; }

        // StudentId is deliberately NOT accepted from the caller: it is taken
        // from the signed-in user so nobody can add skills to another profile.

        public SkillType Type { get; set; } = SkillType.ToTeach;
        public ProficiencyLevel Level { get; set; } = ProficiencyLevel.Beginner;
    }

    public class UpdateSkillDto
    {
        public SkillType? Type { get; set; }
        public ProficiencyLevel? Level { get; set; }
    }

    /// <summary>
    /// What the API returns for a student skill. Never return the EF entity:
    /// its Student navigation is an IdentityUser and would serialise the
    /// password hash and security stamp to the caller.
    /// </summary>
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

    /// <summary>One entry of the shared skill catalogue.</summary>
    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
