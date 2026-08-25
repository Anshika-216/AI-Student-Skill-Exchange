using AIstudentskillexchange.Models;
using AIstudentskillexchange.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AIstudentskillexchange.Controllers;

/// <summary>
/// Entry point of the AI Recommendation Module.
///
/// Acceptance Criteria (Requirement Analysis section 9):
///   1. The student must be logged in           -> [Authorize]
///   2. Must have at least one learning goal    -> handled in the view model
///   3. The system analyses relevant skills     -> ISkillAnalysisService
///   4. Suitable peers are identified           -> IRecommendationService
///   5. Recommended peers are displayed         -> Views/Recommendations/Index
///   6. Match score and reason for each         -> MatchScore + Reasons/AiExplanation
/// </summary>
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

    // GET: /Recommendations?skillId=3
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

    // GET: /Recommendations/Api?skillId=3&take=5
    // JSON endpoint so a dashboard widget or any future front end can pull the
    // same ranked list without rendering the full page.
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
