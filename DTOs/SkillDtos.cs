namespace AIstudentskillexchange.DTOs
{
    public class CreateSkillDto
    {
        public int SkillId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string Type { get; set; } = "ToTeach";
        public string Level { get; set; } = "Beginner";
    }

    public class UpdateSkillDto
    {
        public string? Type { get; set; }
        public string? Level { get; set; }
    }
}