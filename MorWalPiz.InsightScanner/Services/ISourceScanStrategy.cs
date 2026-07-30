using MorWalPiz.Contracts.DTOs;

namespace MorWalPiz.InsightScanner.Services
{
    /// <summary>
    /// Strategy for collecting candidate posts from a single source URL.
    /// </summary>
    public interface ISourceScanStrategy
    {
        /// <summary>
        /// Returns true when this strategy can handle the given source URL.
        /// </summary>
        bool CanHandle(string sourceUrl);

        /// <summary>
        /// Collects up to maxPosts candidate posts, newest first.
        /// </summary>
        Task<List<RawSocialPostDto>> CollectPostsAsync(string sourceUrl, int maxPosts, CancellationToken cancellationToken);
    }
}
