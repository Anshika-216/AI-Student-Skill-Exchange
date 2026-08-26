using AIstudentskillexchange.Services.AI;

namespace AIstudentskillexchange.Models.ViewModels
{
    public class MatchedSkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public ProficiencyLevel MentorLevel { get; set; }

        public ProficiencyLevel LearnerLevel { get; set; }

        public bool IsDirectMatch { get; set; } = true;

        public double Similarity { get; set; } = 1.0;

        public string? RelatedToSkillName { get; set; }

        public string? RelationReason { get; set; }

        public int LevelGap => (int)MentorLevel - (int)LearnerLevel;
    }

    public class ScoreBreakdown
    {
        public double SkillMatch { get; set; }
        public double RelatedSkillMatch { get; set; }
        public double Proficiency { get; set; }
        public double Reciprocity { get; set; }
        public double Rating { get; set; }
        public double Experience { get; set; }
    }

    public class MentorRecommendationViewModel
    {
        public string MentorId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }

        public double MatchScore { get; set; }

        public ScoreBreakdown Breakdown { get; set; } = new();

        public List<MatchedSkillViewModel> DirectMatches { get; set; } = new();

        public List<MatchedSkillViewModel> RelatedMatches { get; set; } = new();

        public IEnumerable<MatchedSkillViewModel> AllMatches => DirectMatches.Concat(RelatedMatches);

        public List<string> ReciprocalSkills { get; set; } = new();

        public double? AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int CompletedSessions { get; set; }

        public bool HasOpenRequest { get; set; }

        public List<string> Reasons { get; set; } = new();

        public string? AiExplanation { get; set; }

        public bool IsMutualExchange => ReciprocalSkills.Count > 0;
    }

    public class SuggestedSkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int AvailableMentors { get; set; }
        public int LearnersInterested { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class RecommendationsViewModel
    {
        public List<MentorRecommendationViewModel> Recommendations { get; set; } = new();
        public List<SuggestedSkillViewModel> SuggestedSkills { get; set; } = new();

        public List<LearningPathStep> LearningPath { get; set; } = new();

        public List<string> Keywords { get; set; } = new();

        public bool AnalysisFromLlm { get; set; }

        public string? AnalysisNotice { get; set; }

        public List<Skill> LearnerWishlist { get; set; } = new();

        public int? FilterSkillId { get; set; }

        public bool WishlistIsEmpty => LearnerWishlist.Count == 0;
    }
}
