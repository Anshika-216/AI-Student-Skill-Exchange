using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.DTOs;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SkillsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static StudentSkillDto ToDto(StudentSkill s) => new()
        {
            Id = s.Id,
            StudentId = s.StudentId,
            StudentName = s.Student?.FullName ?? string.Empty,
            SkillId = s.SkillId,
            SkillName = s.Skill?.Name ?? string.Empty,
            Category = s.Skill?.Category ?? string.Empty,
            Type = s.Type,
            Level = s.Level
        };

        [HttpGet("catalogue")]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetCatalogue(CancellationToken cancellationToken)
        {
            var skills = await _context.Skills
                .AsNoTracking()
                .OrderBy(s => s.Category).ThenBy(s => s.Name)
                .Select(s => new SkillDto { Id = s.Id, Name = s.Name, Category = s.Category })
                .ToListAsync(cancellationToken);

            return Ok(skills);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentSkillDto>>> GetMySkills(CancellationToken cancellationToken)
        {
            var studentId = _userManager.GetUserId(User)!;

            var skills = await _context.StudentSkills
                .AsNoTracking()
                .Include(s => s.Skill)
                .Where(s => s.StudentId == studentId)
                .ToListAsync(cancellationToken);

            return Ok(skills.Select(ToDto));
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<StudentSkillDto>>> GetSkillsByStudent(
            string studentId, CancellationToken cancellationToken)
        {
            var skills = await _context.StudentSkills
                .AsNoTracking()
                .Include(s => s.Skill)
                .Where(s => s.StudentId == studentId)
                .ToListAsync(cancellationToken);

            return Ok(skills.Select(ToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudentSkillDto>> GetSkillById(int id, CancellationToken cancellationToken)
        {
            var skill = await _context.StudentSkills
                .AsNoTracking()
                .Include(s => s.Skill)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (skill == null)
                return NotFound(new { message = "Student skill not found." });

            return Ok(ToDto(skill));
        }

        [HttpPost]
        public async Task<ActionResult<StudentSkillDto>> AddSkill(
            CreateSkillDto dto, CancellationToken cancellationToken)
        {
            var studentId = _userManager.GetUserId(User)!;

            var skill = await _context.Skills.FindAsync([dto.SkillId], cancellationToken);
            if (skill == null)
                return NotFound(new { message = "Skill not found." });

            var alreadyListed = await _context.StudentSkills.AnyAsync(
                s => s.StudentId == studentId && s.SkillId == dto.SkillId && s.Type == dto.Type,
                cancellationToken);

            if (alreadyListed)
                return Conflict(new { message = "That skill is already on your profile for this purpose." });

            var studentSkill = new StudentSkill
            {
                StudentId = studentId,
                SkillId = dto.SkillId,
                Type = dto.Type,
                Level = dto.Level
            };

            _context.StudentSkills.Add(studentSkill);
            await _context.SaveChangesAsync(cancellationToken);

            studentSkill.Skill = skill;

            return CreatedAtAction(
                nameof(GetSkillById),
                new { id = studentSkill.Id },
                ToDto(studentSkill));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSkill(
            int id, UpdateSkillDto dto, CancellationToken cancellationToken)
        {
            var studentId = _userManager.GetUserId(User)!;

            var studentSkill = await _context.StudentSkills
                .Include(s => s.Skill)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (studentSkill == null)
                return NotFound(new { message = "Student skill not found." });

            if (studentSkill.StudentId != studentId)
                return Forbid();

            if (dto.Type.HasValue)
                studentSkill.Type = dto.Type.Value;

            if (dto.Level.HasValue)
                studentSkill.Level = dto.Level.Value;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToDto(studentSkill));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkill(int id, CancellationToken cancellationToken)
        {
            var studentId = _userManager.GetUserId(User)!;

            var studentSkill = await _context.StudentSkills
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (studentSkill == null)
                return NotFound(new { message = "Student skill not found." });

            if (studentSkill.StudentId != studentId)
                return Forbid();

            _context.StudentSkills.Remove(studentSkill);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Skill removed successfully." });
        }
    }
}
