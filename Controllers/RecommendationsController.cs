using AIstudentskillexchange.Models;
using AIstudentskillexchange.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AIstudentskillexchange.Controllers;

[Authorize]
public class RecommendationsController : Controller
{
    private readonly IRecommendationService _recommendationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RecommendationsController(
        IRecommendationService recommendationService,
        UserManager<ApplicationUser> userManager)
    {
        _recommendationService = recommendationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? skillId, CancellationToken cancellationToken)
    {
        var learnerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(learnerId))
            return Challenge();

        var model = await _recommendationService.GetRecommendationsAsync(
            learnerId, skillId, cancellationToken);

        ViewData["Title"] = "Recommended Peers";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Api(int? skillId, int? take, CancellationToken cancellationToken)
    {
        var learnerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(learnerId))
            return Unauthorized();

        var recommendations = await _recommendationService.GetMentorRecommendationsAsync(
            learnerId, skillId, take, cancellationToken);

        return Json(recommendations);
    }
}
