using System.ComponentModel.DataAnnotations;

namespace AIstudentskillexchange.Models
{
    public class Skill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string Category { get; set; } = string.Empty;

        public ICollection<StudentSkill> StudentSkills { get; set; } = new List<StudentSkill>();
    }
}
