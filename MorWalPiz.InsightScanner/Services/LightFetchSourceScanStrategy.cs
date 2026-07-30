using System.Net.Http;
using System.Text.RegularExpressions;
using MorWalPiz.Contracts.DTOs;

namespace MorWalPiz.InsightScanner.Services
{
    /// <summary>
    /// Fallback strategy for generic public sources (blogs, news pages, RSS-like landing pages)
    /// that expose Open Graph metadata and do not require an authenticated/interactive session.
    /// Not suitable for Instagram, which requires <see cref="InteractiveBrowserScanStrategy"/>.
    /// </summary>
    public class LightFetchSourceScanStrategy : ISourceScanStrategy
    {
        private static readonly HttpClient HttpClient = new(new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromSeconds(15) })
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private static readonly Regex OgTitleRegex = new("""<meta[^>]+property=["']og:title["'][^>]+content=["']([^"']*)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex OgDescriptionRegex = new("""<meta[^>]+property=["']og:description["'][^>]+content=["']([^"']*)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public bool CanHandle(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return false;

            return !sourceUrl.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<RawSocialPostDto>> CollectPostsAsync(string sourceUrl, int maxPosts, CancellationToken cancellationToken)
        {
            HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (compatible; MorWalPizInsightScanner/1.0)");

            var html = await HttpClient.GetStringAsync(sourceUrl, cancellationToken);

            var title = OgTitleRegex.Match(html) is { Success: true } titleMatch ? titleMatch.Groups[1].Value : sourceUrl;
            var description = OgDescriptionRegex.Match(html) is { Success: true } descriptionMatch ? descriptionMatch.Groups[1].Value : string.Empty;

            // Light-fetch mode only surfaces the page itself as a single candidate post; the user
            // reviews and, when relevant, forwards it for AI classification alongside browser-collected posts.
            return
            [
                new RawSocialPostDto
                {
                    PostUrl = sourceUrl,
                    PostId = sourceUrl,
                    PlatformSource = "Web",
                    Author = string.Empty,
                    Text = string.IsNullOrWhiteSpace(description) ? title : $"{title}\n{description}",
                    PublishedAt = null
                }
            ];
        }
    }
}
