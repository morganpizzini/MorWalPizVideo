using System.Text.Json;
using MorWalPiz.Contracts.DTOs;

namespace MorWalPiz.InsightScanner.Services
{
    /// <summary>
    /// Best-effort DOM scraper executed inside the interactive WebView2 session after the user has
    /// manually logged in and navigated to a profile/page. Instagram's markup changes frequently and
    /// is not a stable public contract, so this heuristic extraction is expected to need periodic
    /// maintenance; it purposefully only reads what is already rendered in the authenticated session
    /// the user is driving, it does not perform any automated login or bypass of Instagram's access controls.
    /// </summary>
    public static class InstagramDomExtractor
    {
        /// <summary>
        /// JS snippet returning a JSON array of { postUrl, text } for anchors that look like post/reel links
        /// currently rendered on the page. Intended to run via CoreWebView2.ExecuteScriptAsync, which returns
        /// a JSON-encoded string that must be deserialized twice (outer ExecuteScriptAsync encoding + our JSON).
        /// </summary>
        public static string GetExtractionScript(int maxPosts) => $$"""
            (function() {
                const anchors = Array.from(document.querySelectorAll('a[href*="/p/"], a[href*="/reel/"]'));
                const seen = new Set();
                const posts = [];
                for (const a of anchors) {
                    const href = a.href.split('?')[0];
                    if (seen.has(href)) continue;
                    seen.add(href);
                    const img = a.querySelector('img');
                    const text = (img && img.alt) ? img.alt : (a.textContent || '').trim();
                    posts.push({ postUrl: href, text: text.substring(0, 500) });
                    if (posts.length >= {{maxPosts}}) break;
                }
                return JSON.stringify(posts);
            })();
            """;

        public static List<RawSocialPostDto> ParseExtractionResult(string executeScriptResult, string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(executeScriptResult) || executeScriptResult == "null")
                return [];

            // ExecuteScriptAsync wraps the returned string in an extra layer of JSON encoding.
            var json = JsonSerializer.Deserialize<string>(executeScriptResult) ?? executeScriptResult;

            var rawPosts = JsonSerializer.Deserialize<List<ExtractedPost>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];

            return rawPosts
                .Where(p => !string.IsNullOrWhiteSpace(p.PostUrl))
                .Select(p => new RawSocialPostDto
                {
                    PostUrl = p.PostUrl,
                    PostId = p.PostUrl,
                    PlatformSource = "Instagram",
                    Author = string.Empty,
                    Text = p.Text ?? string.Empty,
                    PublishedAt = null
                })
                .ToList();
        }

        private class ExtractedPost
        {
            public string PostUrl { get; set; } = string.Empty;
            public string? Text { get; set; }
        }
    }
}
