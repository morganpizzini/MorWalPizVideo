using MorWalPiz.Contracts.DTOs;

namespace MorWalPiz.InsightScanner.Services
{
    /// <summary>
    /// Orchestrates source collection: routes sources to an automatic strategy (light HTTP fetch)
    /// when possible, or flags that a source requires the interactive WebView2 browser
    /// (currently Instagram, which needs a logged-in, JS-rendered session).
    /// </summary>
    public class HybridInsightScanner
    {
        private readonly IReadOnlyList<ISourceScanStrategy> _automaticStrategies;

        public HybridInsightScanner(IEnumerable<ISourceScanStrategy> automaticStrategies)
        {
            _automaticStrategies = automaticStrategies.ToList();
        }

        public bool RequiresInteractiveBrowser(string sourceUrl) =>
            !string.IsNullOrWhiteSpace(sourceUrl) && sourceUrl.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);

        public async Task<List<RawSocialPostDto>> CollectAutomaticallyAsync(string sourceUrl, int maxPosts, CancellationToken cancellationToken)
        {
            var strategy = _automaticStrategies.FirstOrDefault(s => s.CanHandle(sourceUrl));
            if (strategy == null)
            {
                throw new InvalidOperationException(
                    $"No automatic strategy available for source '{sourceUrl}'. Use the interactive browser tab instead.");
            }

            return await strategy.CollectPostsAsync(sourceUrl, maxPosts, cancellationToken);
        }
    }
}
