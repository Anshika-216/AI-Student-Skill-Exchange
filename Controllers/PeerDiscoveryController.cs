using AIstudentskillexchange.Models;
using AIstudentskillexchange.Models.ViewModels.PeerSearch;
using AIstudentskillexchange.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AIstudentskillexchange.Controllers;

/// <summary>
/// Peer Discovery and Skill Matching Module.
///
/// Requirement Analysis mapping:
///   §5  Student: "Search for other students."
///   §11 "Peer discovery -> Search module"
///   §6  Security: only authorised users may search, hence [Authorize]
///   §6  Privacy: results expose only FullName, Bio and listed skills
/// </summary>
[Authorize]
public class PeerDiscoveryController : Controller
{
    private readonly IPeerSearchService _peerSearchService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PeerDiscoveryController(
        IPeerSearchService peerSearchService,
        UserManager<ApplicationUser> userManager)
    {
        _peerSearchService = peerSearchService;
        _userManager = userManager;
    }

    // GET: /PeerDiscovery?query=python&skillId=3&sort=BestMatch&page=2
    //
    // Criteria are bound from the query string rather than posted, so every
    // search is a shareable and bookmarkable URL and paging links are plain
    // GET links.
    public async Task<IActionResult> Index(
        [FromQuery] PeerSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var viewerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(viewerId))
            return Challenge();

        var model = await _peerSearchService.SearchAsync(viewerId, criteria, cancellationToken);

        ViewData["Title"] = "Find Peers";
        return View(model);
    }

    // GET: /PeerDiscovery/Profile/{id}
    public async Task<IActionResult> Profile(string id, CancellationToken cancellationToken)
    {
        var viewerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(viewerId))
            return Challenge();

        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var peer = await _peerSearchService.GetPeerProfileAsync(viewerId, id, cancellationToken);
        if (peer == null)
            return NotFound();

        ViewData["Title"] = peer.FullName;
        return View(peer);
    }

    // GET: /PeerDiscovery/Api?query=python&page=1
    // JSON endpoint so a future dashboard widget or type-ahead box can reuse the
    // same search without rendering the whole page.
    [HttpGet]
    public async Task<IActionResult> Api(
        [FromQuery] PeerSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var viewerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(viewerId))
            return Unauthorized();

        var model = await _peerSearchService.SearchAsync(viewerId, criteria, cancellationToken);

        return Json(new
        {
            total = model.TotalResults,
            page = model.Page,
            totalPages = model.TotalPages,
            results = model.Results
        });
    }
}
