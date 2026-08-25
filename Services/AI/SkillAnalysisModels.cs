namespace AIstudentskillexchange.Services.AI
{
    /// <summary>
    /// One skill from the catalogue that the AI judged to be related to a skill
    /// the learner asked for. This is what lets the platform go beyond exact
    /// name matching (Requirement Analysis, section 1 and section 5).
    /// </summary>
    public class RelatedSkill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;

        /// <summary>How close the AI judged this skill to be, 0-1.</summary>
        public double Similarity { get; set; }

        /// <summary>Short reason, e.g. "Both are front-end JavaScript frameworks".</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// A single step in the optional learning path (Requirement Analysis,
    /// section 5: "Provide optional learning-path recommendations").
    /// </summary>
    public class LearningPathStep
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;

        /// <summary>Catalogue skill this step maps to, when the AI could match one.</summary>
        public int? SkillId { get; set; }
    }

    /// <summary>
    /// Output of the "AI Skill Analysis" stage of the workflow (section 10),
    /// consumed by the "AI Peer Recommendations" stage.
    /// </summary>
    public class SkillAnalysisResult
    {
        /// <summary>Keyed by the learner wish-list skill id, the related skills found.</summary>
        public Dictionary<int, List<RelatedSkill>> RelatedSkills { get; set; } = new();

        /// <summary>Normalised topic keywords pulled out of the student descriptions.</summary>
        public List<string> Keywords { get; set; } = new();

        public List<LearningPathStep> LearningPath { get; set; } = new();

        /// <summary>True when a real LLM produced this, false when the offline fallback did.</summary>
        public bool FromLlm { get; set; }

        /// <summary>Set when the LLM call failed and the fallback was used instead.</summary>
        public string? FallbackReason { get; set; }

        /// <summary>Flattened lookup of every related skill id to its best similarity.</summary>
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

    /// <summary>
    /// The learner profile handed to the AI Service for analysis.
    /// </summary>
    public class SkillAnalysisRequest
    {
        public string LearnerId { get; set; } = string.Empty;

        /// <summary>Skills the learner wants to learn, with their own descriptions.</summary>
        public List<AnalysedSkillInput> WantsToLearn { get; set; } = new();

        /// <summary>Skills the learner can teach, with their own descriptions.</summary>
        public List<AnalysedSkillInput> CanTeach { get; set; } = new();

        /// <summary>The catalogue the AI is allowed to pick related skills from.</summary>
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

    /// <summary>
    /// A match the AI Service is asked to explain in plain English
    /// (Requirement Analysis section 5: "Generate match scores/explanations").
    /// </summary>
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
