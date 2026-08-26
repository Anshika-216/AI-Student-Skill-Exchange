using NUnit.Framework;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Models.ViewModels;

namespace AIstudentskillexchange.Tests.ViewModels
{
    [TestFixture]
    public class MatchedSkillViewModelTests
    {
        [TestCase(ProficiencyLevel.Expert, ProficiencyLevel.Beginner, 2)]
        [TestCase(ProficiencyLevel.Intermediate, ProficiencyLevel.Beginner, 1)]
        [TestCase(ProficiencyLevel.Beginner, ProficiencyLevel.Beginner, 0)]
        public void LevelGap_IsHowFarTheMentorSitsAboveTheLearner(
            ProficiencyLevel mentor, ProficiencyLevel learner, int expected)
        {
            var match = new MatchedSkillViewModel { MentorLevel = mentor, LearnerLevel = learner };

            Assert.That(match.LevelGap, Is.EqualTo(expected));
        }

        [Test]
        public void LevelGap_IsNegative_WhenTheMentorIsBehindTheLearner()
        {
            var match = new MatchedSkillViewModel
            {
                MentorLevel = ProficiencyLevel.Beginner,
                LearnerLevel = ProficiencyLevel.Expert
            };

            Assert.That(match.LevelGap, Is.EqualTo(-2));
        }

        [Test]
        public void ADirectMatch_IsTheDefaultAndScoresFullSimilarity()
        {
            var match = new MatchedSkillViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(match.IsDirectMatch, Is.True);
                Assert.That(match.Similarity, Is.EqualTo(1.0));
            });
        }
    }

    [TestFixture]
    public class MentorRecommendationViewModelTests
    {
        private static MatchedSkillViewModel Match(string name) => new() { SkillName = name };

        [Test]
        public void AllMatches_CombinesDirectAndRelatedMatches()
        {
            var mentor = new MentorRecommendationViewModel
            {
                DirectMatches = [Match("C#"), Match("SQL")],
                RelatedMatches = [Match("ASP.NET Core")]
            };

            Assert.That(mentor.AllMatches.Select(m => m.SkillName),
                Is.EquivalentTo(new[] { "C#", "SQL", "ASP.NET Core" }));
        }

        [Test]
        public void AllMatches_IsEmpty_WhenNothingMatched()
        {
            Assert.That(new MentorRecommendationViewModel().AllMatches, Is.Empty);
        }

        [Test]
        public void IsMutualExchange_IsTrue_WhenTheMentorWantsSomethingTheLearnerTeaches()
        {
            var mentor = new MentorRecommendationViewModel { ReciprocalSkills = ["Figma"] };

            Assert.That(mentor.IsMutualExchange, Is.True);
        }

        [Test]
        public void IsMutualExchange_IsFalse_WhenTheExchangeIsOneWay()
        {
            Assert.That(new MentorRecommendationViewModel().IsMutualExchange, Is.False);
        }

        [Test]
        public void ANewMentor_StartsWithAnEmptyScoreBreakdown()
        {
            var breakdown = new MentorRecommendationViewModel().Breakdown;

            Assert.Multiple(() =>
            {
                Assert.That(breakdown, Is.Not.Null);
                Assert.That(breakdown.SkillMatch, Is.Zero);
                Assert.That(breakdown.Reciprocity, Is.Zero);
            });
        }
    }

    [TestFixture]
    public class RecommendationsViewModelTests
    {
        [Test]
        public void WishlistIsEmpty_IsTrue_WhenTheStudentHasNoLearningGoal()
        {
            Assert.That(new RecommendationsViewModel().WishlistIsEmpty, Is.True);
        }

        [Test]
        public void WishlistIsEmpty_IsFalse_OnceAGoalIsListed()
        {
            var model = new RecommendationsViewModel
            {
                LearnerWishlist = [new Skill { Id = 1, Name = "C#" }]
            };

            Assert.That(model.WishlistIsEmpty, Is.False);
        }

        [Test]
        public void ANewModel_ReportsTheAnalysisAsComingFromTheOfflineFallback()
        {
            var model = new RecommendationsViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(model.AnalysisFromLlm, Is.False);
                Assert.That(model.Recommendations, Is.Empty);
                Assert.That(model.SuggestedSkills, Is.Empty);
                Assert.That(model.FilterSkillId, Is.Null);
            });
        }
    }
}
