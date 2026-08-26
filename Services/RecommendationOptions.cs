namespace AIstudentskillexchange.Services
{
    public class RecommendationOptions
    {
        public const string SectionName = "Recommendations";

        public double SkillMatchWeight { get; set; } = 0.35;

        public double RelatedSkillWeight { get; set; } = 0.15;

        public double ProficiencyWeight { get; set; } = 0.15;

        public double ReciprocityWeight { get; set; } = 0.20;

        public double RatingWeight { get; set; } = 0.10;

        public double ExperienceWeight { get; set; } = 0.05;

        public double NewMentorRatingBaseline { get; set; } = 0.60;

        public int ExperienceSaturationSessions { get; set; } = 5;

        public double ExistingRequestPenalty { get; set; } = 0.85;

        public double MinimumRelatedSimilarity { get; set; } = 0.35;

        public double MinimumScoreThreshold { get; set; } = 10;

        public int MaxResults { get; set; } = 20;
    }
}
