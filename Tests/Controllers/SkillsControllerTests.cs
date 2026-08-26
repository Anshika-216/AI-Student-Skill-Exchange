using NUnit.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Controllers;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.DTOs;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Tests.TestHelpers;

namespace AIstudentskillexchange.Tests.Controllers
{
    [TestFixture]
    public class SkillsControllerTests
    {
        private const string Alice = "alice-id";
        private const string Bob = "bob-id";

        private ApplicationDbContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _context = TestDb.Create();

            _context.AddUser(Alice, "Alice");
            _context.AddUser(Bob, "Bob");
            _context.AddSkill(1, "C#", "Programming");
            _context.AddSkill(2, "Figma", "Design");
            _context.AddStudentSkill(Alice, 1, SkillType.ToLearn, ProficiencyLevel.Beginner);
            _context.AddStudentSkill(Bob, 1, SkillType.ToTeach, ProficiencyLevel.Expert);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        private SkillsController ControllerAs(string userId) =>
            new SkillsController(_context, FakeUserManager.For(userId)).WithSignedInUser(userId);

        [Test]
        public void Controller_RequiresAuthentication()
        {
            var attribute = typeof(SkillsController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

            Assert.That(attribute, Is.Not.Empty,
                "SkillsController must be [Authorize]d: it exposes and mutates student data.");
        }

        [Test]
        public async Task GetMySkills_ReturnsOnlyTheCallersOwnSkills()
        {
            var result = await ControllerAs(Alice).GetMySkills(default);

            var skills = ((result.Result as OkObjectResult)!.Value as IEnumerable<StudentSkillDto>)!.ToList();

            Assert.Multiple(() =>
            {
                Assert.That(skills, Has.Count.EqualTo(1));
                Assert.That(skills[0].StudentId, Is.EqualTo(Alice));
                Assert.That(skills[0].SkillName, Is.EqualTo("C#"));
            });
        }

        [Test]
        public async Task GetCatalogue_ReturnsTheSharedSkillList()
        {
            var result = await ControllerAs(Alice).GetCatalogue(default);

            var skills = ((result.Result as OkObjectResult)!.Value as IEnumerable<SkillDto>)!.ToList();

            Assert.That(skills.Select(s => s.Name), Is.EquivalentTo(new[] { "C#", "Figma" }));
        }

        [Test]
        public async Task GetSkillById_ReturnsNotFound_ForAnUnknownId()
        {
            var result = await ControllerAs(Alice).GetSkillById(9999, default);

            Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetSkillById_NeverExposesTheIdentityUser()
        {
            var own = await _context.StudentSkills.FirstAsync(s => s.StudentId == Alice);

            var result = await ControllerAs(Alice).GetSkillById(own.Id, default);
            var value = (result.Result as OkObjectResult)!.Value;

            Assert.That(value, Is.TypeOf<StudentSkillDto>(),
                "Returning the EF entity would serialise ApplicationUser, including PasswordHash.");
        }

        [Test]
        public async Task AddSkill_AttachesTheSkillToTheSignedInUser_NotToAnyIdInThePayload()
        {
            var result = await ControllerAs(Alice)
                .AddSkill(new CreateSkillDto { SkillId = 2, Type = SkillType.ToTeach }, default);

            var created = (result.Result as CreatedAtActionResult)!.Value as StudentSkillDto;

            Assert.That(created!.StudentId, Is.EqualTo(Alice));
        }

        [Test]
        public async Task AddSkill_ReturnsNotFound_WhenTheSkillIsNotInTheCatalogue()
        {
            var result = await ControllerAs(Alice)
                .AddSkill(new CreateSkillDto { SkillId = 404 }, default);

            Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task AddSkill_RejectsTheSameSkillTwiceForTheSamePurpose()
        {
            var result = await ControllerAs(Alice)
                .AddSkill(new CreateSkillDto { SkillId = 1, Type = SkillType.ToLearn }, default);

            Assert.That(result.Result, Is.TypeOf<ConflictObjectResult>());
        }

        [Test]
        public async Task AddSkill_AllowsTheSameSkillForTheOppositePurpose()
        {
            var result = await ControllerAs(Alice)
                .AddSkill(new CreateSkillDto { SkillId = 1, Type = SkillType.ToTeach }, default);

            Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task UpdateSkill_AppliesTheChange_ForTheOwner()
        {
            var own = await _context.StudentSkills.FirstAsync(s => s.StudentId == Alice);

            var result = await ControllerAs(Alice)
                .UpdateSkill(own.Id, new UpdateSkillDto { Level = ProficiencyLevel.Expert }, default);

            Assert.Multiple(async () =>
            {
                Assert.That(result, Is.TypeOf<OkObjectResult>());
                Assert.That((await _context.StudentSkills.FindAsync(own.Id))!.Level,
                    Is.EqualTo(ProficiencyLevel.Expert));
            });
        }

        [Test]
        public async Task UpdateSkill_IsForbidden_ForSomebodyElsesSkill()
        {
            var bobsSkill = await _context.StudentSkills.FirstAsync(s => s.StudentId == Bob);

            var result = await ControllerAs(Alice)
                .UpdateSkill(bobsSkill.Id, new UpdateSkillDto { Level = ProficiencyLevel.Beginner }, default);

            Assert.That(result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        public async Task UpdateSkill_LeavesSomebodyElsesDataUntouched()
        {
            var bobsSkill = await _context.StudentSkills.FirstAsync(s => s.StudentId == Bob);

            await ControllerAs(Alice)
                .UpdateSkill(bobsSkill.Id, new UpdateSkillDto { Level = ProficiencyLevel.Beginner }, default);

            Assert.That((await _context.StudentSkills.FindAsync(bobsSkill.Id))!.Level,
                Is.EqualTo(ProficiencyLevel.Expert));
        }

        [Test]
        public async Task UpdateSkill_OnlyChangesTheFieldsSupplied()
        {
            var own = await _context.StudentSkills.FirstAsync(s => s.StudentId == Alice);

            await ControllerAs(Alice).UpdateSkill(own.Id, new UpdateSkillDto(), default);

            var reloaded = (await _context.StudentSkills.FindAsync(own.Id))!;

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Type, Is.EqualTo(SkillType.ToLearn));
                Assert.That(reloaded.Level, Is.EqualTo(ProficiencyLevel.Beginner));
            });
        }

        [Test]
        public async Task DeleteSkill_RemovesTheCallersOwnSkill()
        {
            var own = await _context.StudentSkills.FirstAsync(s => s.StudentId == Alice);

            var result = await ControllerAs(Alice).DeleteSkill(own.Id, default);

            Assert.Multiple(async () =>
            {
                Assert.That(result, Is.TypeOf<OkObjectResult>());
                Assert.That(await _context.StudentSkills.FindAsync(own.Id), Is.Null);
            });
        }

        [Test]
        public async Task DeleteSkill_IsForbidden_ForSomebodyElsesSkill()
        {
            var bobsSkill = await _context.StudentSkills.FirstAsync(s => s.StudentId == Bob);

            var result = await ControllerAs(Alice).DeleteSkill(bobsSkill.Id, default);

            Assert.Multiple(async () =>
            {
                Assert.That(result, Is.TypeOf<ForbidResult>());
                Assert.That(await _context.StudentSkills.FindAsync(bobsSkill.Id), Is.Not.Null,
                    "Bob's skill must survive Alice's delete attempt.");
            });
        }

        [Test]
        public async Task DeleteSkill_ReturnsNotFound_ForAnUnknownId()
        {
            var result = await ControllerAs(Alice).DeleteSkill(9999, default);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }
    }
}
