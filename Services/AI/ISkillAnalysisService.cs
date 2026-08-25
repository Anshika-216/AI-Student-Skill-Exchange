namespace AIstudentskillexchange.Services.AI
{
    /// <summary>
    /// The "AI Service" actor from the Requirement Analysis (section 4).
    ///
    /// Responsibilities mapped from section 5:
    ///   - Analyse student-provided skill descriptions  -> <see cref="AnalyseAsync"/>
    ///   - Identify related or similar skills           -> <see cref="SkillAnalysisResult.RelatedSkills"/>
    ///   - Provide learning-path recommendations        -> <see cref="SkillAnalysisResult.LearningPath"/>
    ///   - Generate match explanations                  -> <see cref="ExplainMatchesAsync"/>
    ///
    /// Peer ranking itself lives in <see cref="IRecommendationService"/>, which
    /// consumes this analysis.
    /// </summary>
    public interface ISkillAnalysisService
    {
        /// <summary>
        /// Stage "AI Skill Analysis" of the system workflow (section 10).
        /// Must never throw: on any LLM failure it returns an offline fallback
        /// result with <see cref="SkillAnalysisResult.FromLlm"/> set to false.
        /// </summary>
        Task<SkillAnalysisResult> AnalyseAsync(
            SkillAnalysisRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asks the LLM for a one-line, student-friendly explanation of why each
        /// match was recommended. Returns a mentor-id keyed dictionary; mentors
        /// the LLM did not cover simply keep their rule-based reasons.
        /// </summary>
        Task<Dictionary<string, string>> ExplainMatchesAsync(
            IReadOnlyList<MatchExplanationRequest> matches,
            CancellationToken cancellationToken = default);
    }
}
