using AIstudentskillexchange.Models.ViewModels;

namespace AIstudentskillexchange.Services
{
    public interface IRecommendationService
    {
        Task<RecommendationsViewModel> GetRecommendationsAsync(
            string learnerId,
            int? skillId = null,
            CancellationToken cancellationToken = default);

        Task<List<MentorRecommendationViewModel>> GetMentorRecommendationsAsync(
            string learnerId,
            int? skillId = null,
            int? maxResults = null,
            CancellationToken cancellationToken = default);

        Task<List<SuggestedSkillViewModel>> GetSuggestedSkillsAsync(
            string learnerId,
            int take = 6,
            CancellationToken cancellationToken = default);
    }
}
