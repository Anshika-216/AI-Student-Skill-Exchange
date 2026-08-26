using NUnit.Framework;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Models.ViewModels.PeerSearch;

namespace AIstudentskillexchange.Tests.ViewModels
{
    [TestFixture]
    public class PeerMatchLabelTests
    {
        [TestCase(PeerMatchType.ExchangePartner, "Exchange partner")]
        [TestCase(PeerMatchType.Mentor, "Can teach you")]
        [TestCase(PeerMatchType.Learner, "Wants to learn from you")]
        [TestCase(PeerMatchType.StudyBuddy, "Study buddy")]
        [TestCase(PeerMatchType.None, "No direct overlap")]
        public void MatchLabel_DescribesTheRelationship(PeerMatchType type, string expected)
        {
            var result = new PeerResultViewModel { MatchType = type };

            Assert.That(result.MatchLabel, Is.EqualTo(expected));
        }

        [TestCase(PeerMatchType.ExchangePartner, "bg-success")]
        [TestCase(PeerMatchType.Mentor, "bg-primary")]
        [TestCase(PeerMatchType.Learner, "bg-info text-dark")]
        [TestCase(PeerMatchType.StudyBuddy, "bg-warning text-dark")]
        [TestCase(PeerMatchType.None, "bg-secondary")]
        public void MatchBadgeCss_GivesEveryMatchTypeItsOwnBadge(PeerMatchType type, string expected)
        {
            var result = new PeerResultViewModel { MatchType = type };

            Assert.That(result.MatchBadgeCss, Is.EqualTo(expected));
        }

        [Test]
        public void EveryMatchType_HasALabelAndABadge()
        {
            foreach (PeerMatchType type in Enum.GetValues<PeerMatchType>())
            {
                var result = new PeerResultViewModel { MatchType = type };

                Assert.Multiple(() =>
                {
                    Assert.That(result.MatchLabel, Is.Not.Empty, $"label missing for {type}");
                    Assert.That(result.MatchBadgeCss, Is.Not.Empty, $"badge missing for {type}");
                });
            }
        }
    }

    [TestFixture]
    public class PeerSearchCriteriaTests
    {
        [Test]
        public void IsEmpty_IsTrue_WhenNothingHasBeenNarrowedDown()
        {
            Assert.That(new PeerSearchCriteria().IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_IsFalse_WhenFreeTextIsSupplied()
        {
            Assert.That(new PeerSearchCriteria { Query = "python" }.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_IgnoresWhitespaceOnlyText()
        {
            Assert.That(new PeerSearchCriteria { Query = "   " }.IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_IsFalse_WhenASkillIsSelected()
        {
            Assert.That(new PeerSearchCriteria { SkillId = 4 }.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_IsFalse_WhenACategoryIsSelected()
        {
            Assert.That(new PeerSearchCriteria { Category = "Programming" }.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_IsFalse_WhenALevelIsSelected()
        {
            Assert.That(new PeerSearchCriteria { Level = ProficiencyLevel.Expert }.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_IsFalse_WhenEitherMatchToggleIsOn()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new PeerSearchCriteria { OnlyMatchingMyGoals = true }.IsEmpty, Is.False);
                Assert.That(new PeerSearchCriteria { OnlyWantingMySkills = true }.IsEmpty, Is.False);
            });
        }

        [Test]
        public void IsEmpty_IgnoresSortAndPage_BecauseNeitherNarrowsTheResults()
        {
            var criteria = new PeerSearchCriteria { Sort = PeerSortOrder.Name, Page = 7 };

            Assert.That(criteria.IsEmpty, Is.True);
        }

        [Test]
        public void DefaultSort_IsBestMatch()
        {
            Assert.That(new PeerSearchCriteria().Sort, Is.EqualTo(PeerSortOrder.BestMatch));
        }
    }

    [TestFixture]
    public class PeerSearchPagingTests
    {
        private static PeerSearchViewModel Model(int total, int page, int pageSize) =>
            new() { TotalResults = total, Page = page, PageSize = pageSize };

        [TestCase(0, 10, 1)]
        [TestCase(1, 10, 1)]
        [TestCase(10, 10, 1)]
        [TestCase(11, 10, 2)]
        [TestCase(25, 10, 3)]
        public void TotalPages_RoundsUpAndNeverDropsBelowOne(int total, int pageSize, int expected)
        {
            Assert.That(Model(total, 1, pageSize).TotalPages, Is.EqualTo(expected));
        }

        [Test]
        public void TotalPages_DoesNotDivideByZero_WhenPageSizeIsUnset()
        {
            Assert.That(Model(50, 1, 0).TotalPages, Is.EqualTo(1));
        }

        [Test]
        public void HasPreviousPage_IsFalseOnTheFirstPage()
        {
            Assert.That(Model(30, 1, 10).HasPreviousPage, Is.False);
        }

        [Test]
        public void HasPreviousPage_IsTrueBeyondTheFirstPage()
        {
            Assert.That(Model(30, 2, 10).HasPreviousPage, Is.True);
        }

        [Test]
        public void HasNextPage_IsTrue_WhilePagesRemain()
        {
            Assert.That(Model(30, 2, 10).HasNextPage, Is.True);
        }

        [Test]
        public void HasNextPage_IsFalseOnTheLastPage()
        {
            Assert.That(Model(30, 3, 10).HasNextPage, Is.False);
        }
    }
}
