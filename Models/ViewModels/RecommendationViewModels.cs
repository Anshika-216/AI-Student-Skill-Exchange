using AIstudentskillexchange.Services.AI;

namespace AIstudentskillexchange.Models.ViewModels
{
    /// <summary>
    /// A skill the mentor teaches that answers something on the learner list -
    /// either directly, or because the AI Service judged it to be related.
    /// </summary>
    public class MatchedSkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        /// <summary>Level the mentor teaches this skill at.</summary>
        public ProficiencyLevel MentorLevel { get; set; }

        /// <summary>Level the learner is currently at for the goal this answers.</summary>
        public ProficiencyLevel LearnerLevel { get; set; }

        /// <summary>False when the AI matched this as a related/similar skill rather than an exact one.</summary>
        public bool IsDirectMatch { get; set; } = true;

        /// <summary>AI similarity for a related match, 0-1. Always 1 for a direct match.</summary>
        public double Similarity { get; set; } = 1.0;

        /// <summary>For a related match, the goal skill it was matched against.</summary>
        public string? RelatedToSkillName { get; set; }

        /// <summary>AI explanation of why this counts as related.</summary>
        public string? RelationReason { get; set; }

        public int LevelGap => (int)MentorLevel - (int)LearnerLevel;
    }

    /// <summary>
    /// The individual 0-1 signals that produced a match score. Kept on the view
    /// model so the UI can explain how the score was reached.
    /// </summary>
    public class ScoreBreakdown
    {
        public double SkillMatch { get; set; }
        public double RelatedSkillMatch { get; set; }
        public double Proficiency { get; set; }
        public double Reciprocity { get; set; }
        public double Rating { get; set; }
        public double Experience { get; set; }
    }

    /// <summary>
    /// One recommended peer mentor, ranked by <see cref="MatchScore"/>.
    /// </summary>
    public class MentorRecommendationViewModel
    {
        public string MentorId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }

        /// <summary>Final blended score, 0-100 (Acceptance Criteria item 6).</summary>
        public double MatchScore { get; set; }

        public ScoreBreakdown Breakdown { get; set; } = new();

        /// <summary>Exact skills from the learner list that this mentor teaches.</summary>
        public List<MatchedSkillViewModel> DirectMatches { get; set; } = new();

        /// <summary>Related/similar skills surfaced by the AI Service.</summary>
        public List<MatchedSkillViewModel> RelatedMatches { get; set; } = new();

        public IEnumerable<MatchedSkillViewModel> AllMatches => DirectMatches.Concat(RelatedMatches);

        /// <summary>Skills the learner teaches that this mentor wants to learn.</summary>
        public List<string> ReciprocalSkills { get; set; } = new();

        public double? AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int CompletedSessions { get; set; }

        /// <summary>True when a pending/accepted request to this mentor already exists.</summary>
        public bool HasOpenRequest { get; set; }

        /// <summary>Rule-based explanations (Acceptance Criteria item 6).</summary>
        public List<string> Reasons { get; set; } = new();

        /// <summary>LLM-written one-liner, shown above the rule-based reasons when available.</summary>
        public string? AiExplanation { get; set; }

        public bool IsMutualExchange => ReciprocalSkills.Count > 0;
    }

    /// <summary>
    /// A skill the learner has not listed yet but that is in demand or widely
    /// taught. Used for the "explore next" strip.
    /// </summary>
    public class SuggestedSkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int AvailableMentors { get; set; }
        public int LearnersInterested { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Everything the recommendations page needs.
    /// </summary>
    public class RecommendationsViewModel
    {
        public List<MentorRecommendationViewModel> Recommendations { get; set; } = new();
        public List<SuggestedSkillViewModel> SuggestedSkills { get; set; } = new();

        /// <summary>Optional learning-path recommendation from the AI Service.</summary>
        public List<LearningPathStep> LearningPath { get; set; } = new();

        /// <summary>Topic keywords the AI pulled out of the student descriptions.</summary>
        public List<string> Keywords { get; set; } = new();

        /// <summary>True when a real LLM produced the analysis, false when the offline fallback did.</summary>
        public bool AnalysisFromLlm { get; set; }

        /// <summary>Why the fallback was used, shown as a small notice.</summary>
        public string? AnalysisNotice { get; set; }

        /// <summary>Skills the learner marked as "want to learn" (drives the filter).</summary>
        public List<Skill> LearnerWishlist { get; set; } = new();

        /// <summary>Currently applied skill filter, if any.</summary>
        public int? FilterSkillId { get; set; }

        /// <summary>Acceptance Criteria item 2: the student needs at least one learning goal.</summary>
        public bool WishlistIsEmpty => LearnerWishlist.Count == 0;
    }
}
