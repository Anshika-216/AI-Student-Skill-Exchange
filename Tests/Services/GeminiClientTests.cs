using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using AIstudentskillexchange.Services.AI;

namespace AIstudentskillexchange.Tests.Services
{
    [TestFixture]
    public class GeminiClientParseJsonTests
    {
        private sealed class Reply
        {
            public string Name { get; set; } = string.Empty;
            public int Score { get; set; }
        }

        private static Reply? Parse(string? raw) =>
            GeminiClient.ParseJson<Reply>(raw, NullLogger.Instance);

        [Test]
        public void ParsesPlainJson()
        {
            var parsed = Parse("""{"name":"C#","score":7}""");

            Assert.Multiple(() =>
            {
                Assert.That(parsed!.Name, Is.EqualTo("C#"));
                Assert.That(parsed.Score, Is.EqualTo(7));
            });
        }

        [Test]
        public void ParsesJsonWrappedInAMarkdownFence()
        {
            var parsed = Parse("```json\n{\"name\":\"C#\",\"score\":7}\n```");

            Assert.That(parsed!.Name, Is.EqualTo("C#"));
        }

        [Test]
        public void ParsesJsonInAnUnlabelledFence()
        {
            var parsed = Parse("```\n{\"name\":\"Figma\",\"score\":3}\n```");

            Assert.That(parsed!.Name, Is.EqualTo("Figma"));
        }

        [Test]
        public void IsCaseInsensitiveAboutPropertyNames()
        {
            var parsed = Parse("""{"NAME":"SQL","Score":1}""");

            Assert.That(parsed!.Name, Is.EqualTo("SQL"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ReturnsNullForAnEmptyReply(string? raw)
        {
            Assert.That(Parse(raw), Is.Null);
        }

        [Test]
        public void ReturnsNullRatherThanThrowingOnMalformedJson()
        {
            Assert.That(Parse("this is not json at all"), Is.Null);
        }

        [Test]
        public void ReturnsNullRatherThanThrowingOnTruncatedJson()
        {
            Assert.That(Parse("""{"name":"C#","score":"""), Is.Null);
        }

        [Test]
        public void ToleratesSurroundingWhitespace()
        {
            var parsed = Parse("\n\n  {\"name\":\"Docker\"}  \n");

            Assert.That(parsed!.Name, Is.EqualTo("Docker"));
        }
    }

    [TestFixture]
    public class GeminiOptionsTests
    {
        [Test]
        public void IsConfigured_IsFalse_WithoutAnApiKey()
        {
            Assert.That(new GeminiOptions().IsConfigured, Is.False);
        }

        [Test]
        public void IsConfigured_IsFalse_WhenTheModuleIsSwitchedOff()
        {
            var options = new GeminiOptions { ApiKey = "key", Enabled = false };

            Assert.That(options.IsConfigured, Is.False);
        }

        [Test]
        public void IsConfigured_IsFalse_ForABlankKey()
        {
            Assert.That(new GeminiOptions { ApiKey = "   " }.IsConfigured, Is.False);
        }

        [Test]
        public void IsConfigured_IsTrue_WhenEnabledWithAKey()
        {
            Assert.That(new GeminiOptions { ApiKey = "key" }.IsConfigured, Is.True);
        }

        [Test]
        public void DefaultsAreSafeWithoutAnyConfiguration()
        {
            var options = new GeminiOptions();

            Assert.Multiple(() =>
            {
                Assert.That(options.Enabled, Is.True);
                Assert.That(options.Model, Is.Not.Empty);
                Assert.That(options.BaseUrl, Does.StartWith("https://"));
                Assert.That(options.TimeoutSeconds, Is.GreaterThan(0));
            });
        }
    }

    [TestFixture]
    public class SkillAnalysisResultTests
    {
        [Test]
        public void ANewResult_ReportsItselfAsNotComingFromAnLlm()
        {
            Assert.That(new SkillAnalysisResult().FromLlm, Is.False);
        }

        [Test]
        public void Flatten_CollapsesRelatedSkillsAcrossGoals()
        {
            var result = new SkillAnalysisResult
            {
                RelatedSkills = new Dictionary<int, List<RelatedSkill>>
                {
                    [1] = [new RelatedSkill { SkillId = 10, Similarity = 0.5 }],
                    [2] = [new RelatedSkill { SkillId = 11, Similarity = 0.9 }]
                }
            };

            Assert.That(result.Flatten().Keys, Is.EquivalentTo(new[] { 10, 11 }));
        }

        [Test]
        public void Flatten_KeepsTheStrongestSimilarityForADuplicateSkill()
        {
            var result = new SkillAnalysisResult
            {
                RelatedSkills = new Dictionary<int, List<RelatedSkill>>
                {
                    [1] = [new RelatedSkill { SkillId = 10, Similarity = 0.4 }],
                    [2] = [new RelatedSkill { SkillId = 10, Similarity = 0.8 }]
                }
            };

            Assert.That(result.Flatten()[10].Similarity, Is.EqualTo(0.8));
        }

        [Test]
        public void Flatten_IsEmpty_WhenNothingWasRelated()
        {
            Assert.That(new SkillAnalysisResult().Flatten(), Is.Empty);
        }
    }
}
