using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AIstudentskillexchange.DTOs;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/profile")]
    public class ProfileApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileApiController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            return Ok(new ProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Bio = user.Bio,
                Email = user.Email ?? string.Empty
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest(new { message = "Display name cannot be empty." });

            user.FullName = dto.FullName.Trim();
            user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            return Ok(new ProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Bio = user.Bio,
                Email = user.Email ?? string.Empty
            });
        }
    }
}
