namespace AIstudentskillexchange.Services.AI
{
    public class RelatedSkill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;

        public double Similarity { get; set; }

        public string Reason { get; set; } = string.Empty;
    }

    public class LearningPathStep
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;

        public int? SkillId { get; set; }
    }

    public class SkillAnalysisResult
    {
        public Dictionary<int, List<RelatedSkill>> RelatedSkills { get; set; } = new();

        public List<string> Keywords { get; set; } = new();

        public List<LearningPathStep> LearningPath { get; set; } = new();

        public bool FromLlm { get; set; }

        public string? FallbackReason { get; set; }

        public Dictionary<int, RelatedSkill> Flatten()
        {
            var flat = new Dictionary<int, RelatedSkill>();
            foreach (var related in RelatedSkills.Values.SelectMany(list => list))
            {
                if (!flat.TryGetValue(related.SkillId, out var existing) || related.Similarity > existing.Similarity)
                    flat[related.SkillId] = related;
            }
            return flat;
        }
    }

    public class SkillAnalysisRequest
    {
        public string LearnerId { get; set; } = string.Empty;

        public List<AnalysedSkillInput> WantsToLearn { get; set; } = new();

        public List<AnalysedSkillInput> CanTeach { get; set; } = new();

        public List<CatalogSkill> Catalog { get; set; } = new();
    }

    public class AnalysedSkillInput
    {
        public int SkillId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CatalogSkill
    {
        public int SkillId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class MatchExplanationRequest
    {
        public string MentorId { get; set; } = string.Empty;
        public string MentorName { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public List<string> DirectSkills { get; set; } = new();
        public List<string> RelatedSkills { get; set; } = new();
        public List<string> ReciprocalSkills { get; set; } = new();
        public double? AverageRating { get; set; }
        public int CompletedSessions { get; set; }
    }
}
