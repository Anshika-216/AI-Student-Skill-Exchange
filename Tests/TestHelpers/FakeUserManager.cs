using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Tests.TestHelpers
{
    public static class FakeUserManager
    {
        public static UserManager<ApplicationUser> For(string userId, string fullName = "Test User")
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            var manager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var user = new ApplicationUser { Id = userId, FullName = fullName };

            manager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            manager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            return manager.Object;
        }

        public static T WithSignedInUser<T>(this T controller, string userId) where T : ControllerBase
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            return controller;
        }
    }
}
