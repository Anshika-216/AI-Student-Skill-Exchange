using AIstudentskillexchange.Models.ViewModels;

namespace AIstudentskillexchange.Services
{
    /// <summary>
    /// The "AI Peer Recommendations" stage of the system workflow (Requirement
    /// Analysis, section 10). Consumes the AI Skill Analysis and turns it into a
    /// ranked, explained list of peer mentors.
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Full recommendation run for one learner: skill analysis, candidate
        /// discovery, scoring, ranking, explanations and learning path.
        /// </summary>
        /// <param name="learnerId">Identity id of the student asking for recommendations.</param>
        /// <param name="skillId">Optional filter: only match on this one learning goal.</param>
        Task<RecommendationsViewModel> GetRecommendationsAsync(
            string learnerId,
            int? skillId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Just the ranked mentor list, for the JSON endpoint and any future
        /// dashboard widget.
        /// </summary>
        Task<List<MentorRecommendationViewModel>> GetMentorRecommendationsAsync(
            string learnerId,
            int? skillId = null,
            int? maxResults = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Suggests skills the learner has not listed yet, based on what the
        /// community teaches and what their peers are learning.
        /// </summary>
        Task<List<SuggestedSkillViewModel>> GetSuggestedSkillsAsync(
            string learnerId,
            int take = 6,
            CancellationToken cancellationToken = default);
    }
}
