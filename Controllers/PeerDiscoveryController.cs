using AIstudentskillexchange.Models;
using AIstudentskillexchange.Models.ViewModels.PeerSearch;
using AIstudentskillexchange.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AIstudentskillexchange.Controllers;

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
