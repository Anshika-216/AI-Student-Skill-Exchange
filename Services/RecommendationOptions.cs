namespace AIstudentskillexchange.Services
{
    /// <summary>
    /// Tunable weights for the peer-ranking stage of the AI Recommendation Module.
    /// Bound from the "Recommendations" section of appsettings.json so matching
    /// behaviour can be tuned without recompiling.
    /// </summary>
    public class RecommendationOptions
    {
        public const string SectionName = "Recommendations";

        // --- Signal weights (normalised at runtime, so they need not sum to 1) ---

        /// <summary>Exact skills from the learner wish-list that the mentor teaches.</summary>
        public double SkillMatchWeight { get; set; } = 0.35;

        /// <summary>Related/similar skills identified by the AI Service.</summary>
        public double RelatedSkillWeight { get; set; } = 0.15;

        /// <summary>How far the mentor proficiency sits above the learner level.</summary>
        public double ProficiencyWeight { get; set; } = 0.15;

        /// <summary>Two-way exchange: the mentor also wants to learn something the learner teaches.</summary>
        public double ReciprocityWeight { get; set; } = 0.20;

        /// <summary>Average feedback rating the mentor earned as a teacher.</summary>
        public double RatingWeight { get; set; } = 0.10;

        /// <summary>Number of sessions the mentor has actually completed.</summary>
        public double ExperienceWeight { get; set; } = 0.05;

        // --- Behaviour knobs ---

        /// <summary>Neutral rating given to mentors who have no feedback yet (0-1).</summary>
        public double NewMentorRatingBaseline { get; set; } = 0.60;

        /// <summary>Completed sessions needed before the experience signal maxes out.</summary>
        public int ExperienceSaturationSessions { get; set; } = 5;

        /// <summary>Multiplier applied when a request to this mentor is already open.</summary>
        public double ExistingRequestPenalty { get; set; } = 0.85;

        /// <summary>AI similarity below which a related skill is ignored entirely.</summary>
        public double MinimumRelatedSimilarity { get; set; } = 0.35;

        /// <summary>Matches scoring below this (out of 100) are dropped from the results.</summary>
        public double MinimumScoreThreshold { get; set; } = 10;

        /// <summary>Maximum number of mentors returned in one page of results.</summary>
        public int MaxResults { get; set; } = 20;
    }
}
