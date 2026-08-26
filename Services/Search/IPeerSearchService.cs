using AIstudentskillexchange.Models.ViewModels.PeerSearch;

namespace AIstudentskillexchange.Services.Search
{
    /// <summary>
    /// Peer Discovery and Skill Matching Module (Requirement Analysis §11,
    /// "Peer discovery -> Search module").
    ///
    /// Covers the student requirement "Search for other students" (§5) and the
    /// skill-matching rules that decide what each result is useful for.
    ///
    /// This is the deterministic, user-driven half of finding a partner: the
    /// student states what they are looking for and gets exactly that. It is
    /// deliberately separate from any AI-driven recommendation feature, which
    /// pushes suggestions without the student asking.
    /// </summary>
    public interface IPeerSearchService
    {
        /// <summary>
        /// Runs a filtered, sorted, paged search for peers and works out how each
        /// result relates to the searching student.
        /// </summary>
        /// <param name="viewerId">Identity id of the student running the search.</param>
        /// <param name="criteria">Filters bound from the query string.</param>
        Task<PeerSearchViewModel> SearchAsync(
            string viewerId,
            PeerSearchCriteria criteria,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a single peer's full public profile, for the result "view profile"
        /// link. Returns null when the id does not exist or is the viewer.
        /// </summary>
        Task<PeerResultViewModel?> GetPeerProfileAsync(
            string viewerId,
            string peerId,
            CancellationToken cancellationToken = default);
    }
}
