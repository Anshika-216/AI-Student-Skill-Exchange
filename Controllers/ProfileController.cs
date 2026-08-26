using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIstudentskillexchange.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "My Skills";
            return View();
        }
    }
}
