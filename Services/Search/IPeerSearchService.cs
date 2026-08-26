using AIstudentskillexchange.Models.ViewModels.PeerSearch;

namespace AIstudentskillexchange.Services.Search
{
    public interface IPeerSearchService
    {
        Task<PeerSearchViewModel> SearchAsync(
            string viewerId,
            PeerSearchCriteria criteria,
            CancellationToken cancellationToken = default);

        Task<PeerResultViewModel?> GetPeerProfileAsync(
            string viewerId,
            string peerId,
            CancellationToken cancellationToken = default);
    }
}
