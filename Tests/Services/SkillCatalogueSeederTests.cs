using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.Tests.TestHelpers;

namespace AIstudentskillexchange.Tests.Services
{
    [TestFixture]
    public class SkillCatalogueSeederTests
    {
        [Test]
        public async Task SeedAsync_OnAnEmptyDatabase_FillsTheCatalogue()
        {
            using var context = TestDb.Create();

            await SkillCatalogueSeeder.SeedAsync(context);

            Assert.That(await context.Skills.CountAsync(), Is.GreaterThan(20));
        }

        [Test]
        public async Task SeedAsync_GivesEverySkillANameAndACategory()
        {
            using var context = TestDb.Create();

            await SkillCatalogueSeeder.SeedAsync(context);

            var skills = await context.Skills.ToListAsync();

            Assert.Multiple(() =>
            {
                Assert.That(skills.Select(s => s.Name), Is.All.Not.Empty);
                Assert.That(skills.Select(s => s.Category), Is.All.Not.Empty);
            });
        }

        [Test]
        public async Task SeedAsync_ProducesNoDuplicateNames()
        {
            using var context = TestDb.Create();

            await SkillCatalogueSeeder.SeedAsync(context);

            var names = await context.Skills.Select(s => s.Name).ToListAsync();

            Assert.That(names, Is.Unique);
        }

        [Test]
        public async Task SeedAsync_IsSafeToRunTwice()
        {
            using var context = TestDb.Create();

            await SkillCatalogueSeeder.SeedAsync(context);
            var afterFirst = await context.Skills.CountAsync();

            await SkillCatalogueSeeder.SeedAsync(context);

            Assert.That(await context.Skills.CountAsync(), Is.EqualTo(afterFirst),
                "The seeder runs on every startup, so a second pass must add nothing.");
        }

        [Test]
        public async Task SeedAsync_LeavesSkillsAddedByHandAlone()
        {
            using var context = TestDb.Create();
            context.AddSkill(500, "Underwater Basket Weaving", "Crafts");
            await context.SaveChangesAsync();

            await SkillCatalogueSeeder.SeedAsync(context);

            Assert.That(await context.Skills.AnyAsync(s => s.Id == 500), Is.True);
        }

        [Test]
        public async Task SeedAsync_DoesNotReAddASkillThatAlreadyExists()
        {
            using var context = TestDb.Create();
            context.AddSkill(500, "Python", "Programming");
            await context.SaveChangesAsync();

            await SkillCatalogueSeeder.SeedAsync(context);

            Assert.That(await context.Skills.CountAsync(s => s.Name == "Python"), Is.EqualTo(1));
        }

        [Test]
        public async Task SeedAsync_MatchesExistingNamesCaseInsensitively()
        {
            using var context = TestDb.Create();
            context.AddSkill(500, "pYtHoN", "Programming");
            await context.SaveChangesAsync();

            await SkillCatalogueSeeder.SeedAsync(context);

            Assert.That(await context.Skills.CountAsync(s => s.Name.ToLower() == "python"),
                Is.EqualTo(1));
        }

        [Test]
        public async Task SeedAsync_CoversSeveralCategories()
        {
            using var context = TestDb.Create();

            await SkillCatalogueSeeder.SeedAsync(context);

            var categories = await context.Skills.Select(s => s.Category).Distinct().ToListAsync();

            Assert.That(categories, Has.Count.GreaterThan(3));
        }
    }
}
