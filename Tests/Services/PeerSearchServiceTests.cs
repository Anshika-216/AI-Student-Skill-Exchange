using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Models.ViewModels.PeerSearch;
using AIstudentskillexchange.Services.Search;
using AIstudentskillexchange.Tests.TestHelpers;

namespace AIstudentskillexchange.Tests.Services
{
    [TestFixture]
    public class PeerSearchServiceTests
    {
        private const string Viewer = "viewer-id";
        private const string Mentor = "mentor-id";
        private const string Learner = "learner-id";
        private const string Buddy = "buddy-id";
        private const string Stranger = "stranger-id";

        private ApplicationDbContext _context = null!;
        private PeerSearchService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _context = TestDb.Create();

            _context.AddSkill(1, "C#", "Programming");
            _context.AddSkill(2, "Figma", "Design");
            _context.AddSkill(3, "Spanish", "Language");

            _context.AddUser(Viewer, "Viewer");
            _context.AddStudentSkill(Viewer, 1, SkillType.ToLearn, ProficiencyLevel.Beginner);
            _context.AddStudentSkill(Viewer, 2, SkillType.ToTeach, ProficiencyLevel.Expert);

            _context.AddUser(Mentor, "Mentor");
            _context.AddStudentSkill(Mentor, 1, SkillType.ToTeach, ProficiencyLevel.Expert);

            _context.AddUser(Learner, "Learner");
            _context.AddStudentSkill(Learner, 2, SkillType.ToLearn, ProficiencyLevel.Beginner);

            _context.AddUser(Buddy, "Buddy");
            _context.AddStudentSkill(Buddy, 1, SkillType.ToLearn, ProficiencyLevel.Beginner);

            _context.AddUser(Stranger, "Stranger");
            _context.AddStudentSkill(Stranger, 3, SkillType.ToTeach, ProficiencyLevel.Expert);

            _context.SaveChanges();

            _service = new PeerSearchService(
                _context,
                Options.Create(new PeerSearchOptions()),
                NullLogger<PeerSearchService>.Instance);
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        private async Task<PeerResultViewModel?> FindAsync(string peerId, PeerSearchCriteria? criteria = null)
        {
            var model = await _service.SearchAsync(Viewer, criteria ?? new PeerSearchCriteria());
            return model.Results.FirstOrDefault(r => r.StudentId == peerId);
        }

        [Test]
        public async Task Search_NeverReturnsTheViewerThemselves()
        {
            var model = await _service.SearchAsync(Viewer, new PeerSearchCriteria());

            Assert.That(model.Results.Select(r => r.StudentId), Does.Not.Contain(Viewer));
        }

        [Test]
        public async Task SomebodyWhoTeachesWhatIWant_IsAMentor()
        {
            var peer = await FindAsync(Mentor);

            Assert.Multiple(() =>
            {
                Assert.That(peer, Is.Not.Null);
                Assert.That(peer!.MatchType, Is.EqualTo(PeerMatchType.Mentor));
                Assert.That(peer.TeachesWhatIWant, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task SomebodyWhoWantsWhatICanTeach_IsALearner()
        {
            var peer = await FindAsync(Learner);

            Assert.Multiple(() =>
            {
                Assert.That(peer!.MatchType, Is.EqualTo(PeerMatchType.Learner));
                Assert.That(peer.WantsWhatICanTeach, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task SomebodyChasingTheSameGoal_IsAStudyBuddy()
        {
            var peer = await FindAsync(Buddy);

            Assert.Multiple(() =>
            {
                Assert.That(peer!.MatchType, Is.EqualTo(PeerMatchType.StudyBuddy));
                Assert.That(peer.SharedGoals, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task ATwoWayOverlap_IsAnExchangePartner()
        {
            const string both = "both-id";
            _context.AddUser(both, "Both");
            _context.AddStudentSkill(both, 1, SkillType.ToTeach, ProficiencyLevel.Expert);
            _context.AddStudentSkill(both, 2, SkillType.ToLearn, ProficiencyLevel.Beginner);
            await _context.SaveChangesAsync();

            var peer = await FindAsync(both);

            Assert.That(peer!.MatchType, Is.EqualTo(PeerMatchType.ExchangePartner));
        }

        [Test]
        public async Task AnExchangePartner_OutranksAOneWayMentor()
        {
            const string both = "both-id";
            _context.AddUser(both, "Both");
            _context.AddStudentSkill(both, 1, SkillType.ToTeach, ProficiencyLevel.Expert);
            _context.AddStudentSkill(both, 2, SkillType.ToLearn, ProficiencyLevel.Beginner);
            await _context.SaveChangesAsync();

            var exchange = await FindAsync(both);
            var mentor = await FindAsync(Mentor);

            Assert.That(exchange!.MatchStrength, Is.GreaterThan(mentor!.MatchStrength));
        }

        [Test]
        public async Task SomebodyWithNoOverlap_HasNoMatchTypeAndScoresZero()
        {
            var peer = await FindAsync(Stranger);

            Assert.Multiple(() =>
            {
                Assert.That(peer!.MatchType, Is.EqualTo(PeerMatchType.None));
                Assert.That(peer.MatchStrength, Is.Zero);
            });
        }

        [Test]
        public async Task MatchStrength_StaysWithinZeroToOneHundred()
        {
            var model = await _service.SearchAsync(Viewer, new PeerSearchCriteria());

            Assert.That(model.Results.Select(r => r.MatchStrength),
                Is.All.InRange(0, 100));
        }

        [Test]
        public async Task OnlyMatchingMyGoals_KeepsPeersWhoTeachWhatIWant()
        {
            var model = await _service.SearchAsync(
                Viewer, new PeerSearchCriteria { OnlyMatchingMyGoals = true });

            Assert.Multiple(() =>
            {
                Assert.That(model.Results.Select(r => r.StudentId), Does.Contain(Mentor));
                Assert.That(model.Results.Select(r => r.StudentId), Does.Not.Contain(Stranger));
            });
        }

        [Test]
        public async Task OnlyWantingMySkills_KeepsPeersWhoWantWhatITeach()
        {
            var model = await _service.SearchAsync(
                Viewer, new PeerSearchCriteria { OnlyWantingMySkills = true });

            Assert.Multiple(() =>
            {
                Assert.That(model.Results.Select(r => r.StudentId), Does.Contain(Learner));
                Assert.That(model.Results.Select(r => r.StudentId), Does.Not.Contain(Stranger));
            });
        }

        [Test]
        public async Task FilteringBySkill_NarrowsToPeopleWithThatSkill()
        {
            var model = await _service.SearchAsync(Viewer, new PeerSearchCriteria { SkillId = 3 });

            Assert.That(model.Results.Select(r => r.StudentId), Is.EqualTo(new[] { Stranger }));
        }

        [Test]
        public async Task FilteringByCategory_NarrowsToThatCategory()
        {
            var model = await _service.SearchAsync(
                Viewer, new PeerSearchCriteria { Category = "Language" });

            Assert.That(model.Results.Select(r => r.StudentId), Is.EqualTo(new[] { Stranger }));
        }

        [Test]
        public async Task TheViewerProfileCounts_AreReportedBackToTheView()
        {
            var model = await _service.SearchAsync(Viewer, new PeerSearchCriteria());

            Assert.Multiple(() =>
            {
                Assert.That(model.ViewerGoalCount, Is.EqualTo(1));
                Assert.That(model.ViewerTeachCount, Is.EqualTo(1));
                Assert.That(model.ViewerHasNoProfile, Is.False);
            });
        }

        [Test]
        public async Task AStudentWithNoSkills_IsToldTheirProfileIsEmpty()
        {
            const string blank = "blank-id";
            _context.AddUser(blank, "Blank");
            await _context.SaveChangesAsync();

            var model = await _service.SearchAsync(blank, new PeerSearchCriteria());

            Assert.That(model.ViewerHasNoProfile, Is.True);
        }

        [Test]
        public async Task GetPeerProfile_ReturnsNullForTheViewer()
        {
            Assert.That(await _service.GetPeerProfileAsync(Viewer, Viewer), Is.Null);
        }

        [Test]
        public async Task GetPeerProfile_ReturnsNullForAnUnknownId()
        {
            Assert.That(await _service.GetPeerProfileAsync(Viewer, "ghost"), Is.Null);
        }

        [Test]
        public async Task GetPeerProfile_ClassifiesTheMatchTheSameWayTheSearchDoes()
        {
            var profile = await _service.GetPeerProfileAsync(Viewer, Mentor);

            Assert.Multiple(() =>
            {
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile!.MatchType, Is.EqualTo(PeerMatchType.Mentor));
                Assert.That(profile.FullName, Is.EqualTo("Mentor"));
            });
        }

        [Test]
        public async Task Paging_ReportsTheTotalAcrossAllPages()
        {
            var model = await _service.SearchAsync(Viewer, new PeerSearchCriteria());

            Assert.That(model.TotalResults, Is.EqualTo(4));
        }
    }
}
