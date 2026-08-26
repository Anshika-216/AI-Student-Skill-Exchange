using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.DTOs
{
    public class CreateSkillDto
    {
        public int SkillId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public SkillType Type { get; set; } = SkillType.ToTeach;
        public ProficiencyLevel Level { get; set; } = ProficiencyLevel.Beginner;
    }

    public class UpdateSkillDto
    {
        public SkillType? Type { get; set; }
        public ProficiencyLevel? Level { get; set; }
    }
}
