using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.DTOs;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SkillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Skills
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentSkill>>> GetAllSkills()
        {
            var skills = await _context.StudentSkills
                .Include(s => s.Skill)
                .ToListAsync();

            return Ok(skills);
        }

        // GET: api/Skills/student/{studentId}
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<StudentSkill>>> GetSkillsByStudent(string studentId)
        {
            var skills = await _context.StudentSkills
                .Include(s => s.Skill)
                .Where(s => s.StudentId == studentId)
                .ToListAsync();

            return Ok(skills);
        }

        // GET: api/Skills/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<StudentSkill>> GetSkillById(int id)
        {
            var skill = await _context.StudentSkills
                .Include(s => s.Skill)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (skill == null)
                return NotFound(new { message = "Student skill not found." });

            return Ok(skill);
        }

        // POST: api/Skills
        [HttpPost]
        public async Task<ActionResult<StudentSkill>> AddSkill(CreateSkillDto dto)
        {
            var skill = await _context.Skills.FindAsync(dto.SkillId);

            if (skill == null)
                return NotFound(new { message = "Skill not found." });

            var studentSkill = new StudentSkill
            {
                StudentId = dto.StudentId,
                SkillId = dto.SkillId,
                Type = dto.Type,
                Level = dto.Level
            };

            _context.StudentSkills.Add(studentSkill);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetSkillById),
                new { id = studentSkill.Id },
                studentSkill
            );
        }

        // PUT: api/Skills/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSkill(
            int id,
            UpdateSkillDto dto)
        {
            var studentSkill = await _context.StudentSkills.FindAsync(id);

            if (studentSkill == null)
                return NotFound(new { message = "Student skill not found." });

            if (dto.Type.HasValue)
                studentSkill.Type = dto.Type.Value;

            if (dto.Level.HasValue)
                studentSkill.Level = dto.Level.Value;

            await _context.SaveChangesAsync();

            return Ok(studentSkill);
        }

        // DELETE: api/Skills/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var studentSkill = await _context.StudentSkills.FindAsync(id);

            if (studentSkill == null)
                return NotFound(new { message = "Student skill not found." });

            _context.StudentSkills.Remove(studentSkill);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Skill removed successfully." });
        }
    }
}