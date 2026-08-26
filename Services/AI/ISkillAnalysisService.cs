namespace AIstudentskillexchange.Services.AI
{
    public interface ISkillAnalysisService
    {
        Task<SkillAnalysisResult> AnalyseAsync(
            SkillAnalysisRequest request,
            CancellationToken cancellationToken = default);

        Task<Dictionary<string, string>> ExplainMatchesAsync(
            IReadOnlyList<MatchExplanationRequest> matches,
            CancellationToken cancellationToken = default);
    }
}
