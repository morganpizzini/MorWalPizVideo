using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using YoutubeContentType = MorWalPizVideo.Server.Models.YoutubeContentType;

namespace MorWalPizVideo.Shortlinks.Controllers
{
    [Route("/")]
    public class ShortLinkController : ApplicationControllerBase
    {
        private IShortLinkDataService _shortlinkDataService;
        private readonly IMorWalPizCache cache;

        public ShortLinkController(IShortLinkDataService shortlinkDataService, IMorWalPizCache memoryCache)
        {
            cache = memoryCache;
            _shortlinkDataService = shortlinkDataService;
        }

        private async Task<ShortLink?> FindShortLinkInContent(string code)
        {
            // First, try to find shortlink in YouTubeContent entities
            var youtubeContents = await FetchMatches();
            var shortLink = youtubeContents.FirstOrDefault(x=>x.GetShortLink(code) != null)?.GetShortLink(code);
            
            if (shortLink != null)
            {
                return shortLink;
            }

            // Second, try to find shortlink in YTChannel entities
            var channels = await FetchChannels();
            shortLink = channels.FirstOrDefault(x=>x.GetShortLink(code) != null)?.GetShortLink(code);
            
            if (shortLink != null)
            {
                return shortLink;
            }

            // Finally, check standalone shortlinks (for non-YouTube content)
            var standaloneShortlinks = await FetchShortlinks();
            return standaloneShortlinks.FirstOrDefault(x => x.Code == code && 
                x.LinkType != LinkType.YouTubeVideo && 
                x.LinkType != LinkType.YouTubeChannel);
        }

        private async Task UpdateShortLinkClickCount(string code, ShortLink shortLink)
        {
            var updatedShortLink = shortLink with { ClicksCount = shortLink.ClicksCount + 1 };

            // Find which entity contains this shortlink and update it
            var youtubeContents = await FetchMatchesWithoutCache();
            var existing = youtubeContents.FirstOrDefault(x => x.GetShortLink(code) != null);
            if(existing != null)
            {
                var updatedContent = existing.UpdateShortLink(code, updatedShortLink);
                await _shortlinkDataService.UpdateYouTubeContent(updatedContent);
                return;
            }

            var channels = await FetchChannelsWithoutCache();
            var existingChannel = channels.FirstOrDefault(x => x.GetShortLink(code) != null);
            if(existingChannel != null) {
                var updatedChannel = existingChannel.UpdateShortLink(code, updatedShortLink);
                await _shortlinkDataService.UpdateYTChannel(updatedChannel);
                return;
            }

            // Update standalone shortlink
            await _shortlinkDataService.UpdateShortlink(updatedShortLink);
        }

        [HttpGet("{videoShortLink}")]
        public async Task<IActionResult> Index(string videoShortLink)
        {
            if (string.IsNullOrWhiteSpace(videoShortLink))
                return BadRequest("Video ID is required.");

            if (videoShortLink == "clear")
            {
                ClearCache();
                return NoContent();
            }

            // Get device information for later use
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            bool isAndroid = userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase);
            bool isIOS = userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                         userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase);

            // Handle "last" special case - always YouTube video
            if (videoShortLink.ToLower() == "last")
            {
                var lastMatch = (await FetchMatches()).FirstOrDefault();
                if (lastMatch == null)
                    return BadRequest("last match cannot found");

                string videoId; 
                if (lastMatch.ContentType == YoutubeContentType.SingleVideo)
                {
                    videoId = lastMatch.ThumbnailVideoId;
                }
                else
                {
                    var selectedVideo = lastMatch.VideoRefs.OrderByDescending(x => x.CreationDateTime).FirstOrDefault();
                    if (selectedVideo == null)
                        return BadRequest("Video not found");
                    videoId = selectedVideo.YoutubeId;
                }
                string lastQueryString = "&list=PLS0u4VTR02qSj1w2YktAft_4Vs52O9adG";
                return RedirectYouTubeVideo(videoId, lastQueryString, isAndroid, isIOS);
            }

            // Normal shortlink handling using the new embedded approach
            var shortLink = await FindShortLinkInContent(videoShortLink);
            if (shortLink == null)
                return BadRequest("shortLink not found");

            // Increment click count
            await UpdateShortLinkClickCount(videoShortLink, shortLink);

            // Handle different link types
            string linkQuerystring = !string.IsNullOrEmpty(shortLink.QueryString) ? $"&{shortLink.QueryString}" : string.Empty;

            // For non-YouTube video types, handle directly based on LinkType
            switch (shortLink.LinkType)
            {
                case LinkType.YouTubeChannel:
                    // Handle YouTube channel links
                    string channelHandle = shortLink.Target; // This could be either '@channelname' or a channel ID
                    string channelWebUrl = $"https://www.youtube.com/{channelHandle}{(!string.IsNullOrEmpty(linkQuerystring) ? $"?{linkQuerystring.TrimStart('&')}" : "")}";

                    if (isAndroid)
                        return Redirect($"vnd.youtube://www.youtube.com/{channelHandle}{(!string.IsNullOrEmpty(linkQuerystring) ? $"?{linkQuerystring.TrimStart('&')}" : "")}");
                    else if (isIOS)
                        return Redirect($"youtube://www.youtube.com/{channelHandle}{(!string.IsNullOrEmpty(linkQuerystring) ? $"?{linkQuerystring.TrimStart('&')}" : "")}");
                    return Redirect(channelWebUrl);

                case LinkType.YouTubePlaylist:
                    // Handle YouTube playlist links
                    string playlistId = shortLink.Target;
                    string playlistWebUrl = $"https://www.youtube.com/playlist?list={playlistId}{(!string.IsNullOrEmpty(linkQuerystring) ? $"&{linkQuerystring.TrimStart('&')}" : "")}";

                    if (isAndroid)
                        return Redirect($"vnd.youtube://playlist?list={playlistId}{(!string.IsNullOrEmpty(linkQuerystring) ? $"&{linkQuerystring.TrimStart('&')}" : "")}");
                    else if (isIOS)
                        return Redirect($"youtube://playlist?list={playlistId}{(!string.IsNullOrEmpty(linkQuerystring) ? $"&{linkQuerystring.TrimStart('&')}" : "")}");
                    return Redirect(playlistWebUrl);
                case LinkType.Instagram:
                    // Handle Instagram link (posts, profiles, etc.)
                    string instagramId = shortLink.Target;

                    // Check if it's a post ID (numeric) or a profile/other content
                    bool isNumeric = long.TryParse(instagramId, out _);

                    if (isNumeric || instagramId.Contains("/p/"))
                    {
                        // Handle as a post - make sure we have just the post ID
                        string postId = instagramId;
                        if (instagramId.Contains("/p/"))
                        {
                            // Extract the post ID from URLs like "instagram.com/p/ABC123"
                            var parts = instagramId.Split(new[] { "/p/" }, StringSplitOptions.None);
                            postId = parts.Length > 1 ? parts[1].TrimEnd('/') : instagramId;
                        }

                        string instagramPostUrl = $"https://www.instagram.com/p/{postId}/";

                        if (isAndroid || isIOS)
                            return Redirect($"instagram://media?id={postId}");
                        return Redirect(instagramPostUrl);
                    }
                    else
                    {
                        // Handle as a profile
                        string username = instagramId.TrimStart('@');
                        string instagramProfileUrl = $"https://www.instagram.com/{username}";

                        if (isAndroid || isIOS)
                            return Redirect($"instagram://user?username={username}");
                        return Redirect(instagramProfileUrl);
                    }

                case LinkType.Facebook:
                    // Handle Facebook links
                    string facebookTarget = shortLink.Target;

                    // Check if it's a post URL or ID
                    if (facebookTarget.Contains("/posts/") || facebookTarget.Contains("facebook.com") || facebookTarget.Contains("fb.watch"))
                    {
                        // Handle as a post
                        string postId = facebookTarget;

                        // Try to extract post ID from common Facebook URL formats
                        if (facebookTarget.Contains("/posts/"))
                        {
                            var parts = facebookTarget.Split(new[] { "/posts/" }, StringSplitOptions.None);
                            postId = parts.Length > 1 ? parts[1].Split('/')[0] : facebookTarget;
                        }
                        else if (facebookTarget.Contains("fb.watch"))
                        {
                            // For short URLs, use as is
                            string fbWatchUrl = facebookTarget.StartsWith("http") ? facebookTarget : $"https://{facebookTarget}";

                            // For mobile apps, we can just use the web URL as Facebook will handle the redirection
                            return Redirect(fbWatchUrl);
                        }

                        string facebookPostUrl = facebookTarget.StartsWith("http") ?
                            facebookTarget :
                            $"https://www.facebook.com/{postId}";

                        if (isAndroid || isIOS)
                            return Redirect($"fb://post/{postId}");
                        return Redirect(facebookPostUrl);
                    }
                    else
                    {
                        // Handle as a profile
                        string profileId = facebookTarget.TrimStart('@');
                        string facebookProfileUrl = $"https://www.facebook.com/{profileId}";

                        if (isAndroid || isIOS)
                            return Redirect($"fb://profile/{profileId}");
                        return Redirect(facebookProfileUrl);
                    }

                case LinkType.CustomUrl:
                    // Direct link to the URL in Target
                    return Redirect(shortLink.Target);

                case LinkType.YouTubeVideo:
                default:                    // For YouTube videos, we need to look up the actual video data
                    var existingMatch = (await FetchMatches()).FirstOrDefault(x => 
                        (x.ContentType == YoutubeContentType.SingleVideo && x.ThumbnailVideoId == shortLink.Target) || 
                        (x.VideoRefs != null && x.VideoRefs.Any(v => v.YoutubeId == shortLink.Target)));
                    
                    if (existingMatch == null)
                        return BadRequest("Video not found");

                    string videoId = string.Empty;
                    if (existingMatch.ContentType == YoutubeContentType.SingleVideo)
                    {
                        videoId = existingMatch.ThumbnailVideoId;
                    }
                    else
                    {
                        var selectedVideo = existingMatch.VideoRefs.FirstOrDefault(x => x.YoutubeId == shortLink.Target);
                        if (selectedVideo == null)
                            return BadRequest("Video shortLink not found");
                        videoId = selectedVideo.YoutubeId;
                    }

                    return RedirectYouTubeVideo(videoId, linkQuerystring, isAndroid, isIOS);
            }
        }

        // Helper method to reduce duplication for YouTube video redirects
        private IActionResult RedirectYouTubeVideo(string videoId, string queryString, bool isAndroid, bool isIOS)
        {
            string webUrl = $"https://www.youtube.com/watch?v={videoId}{queryString}";

            if (isAndroid)
                return Redirect($"vnd.youtube://watch?v={videoId}{queryString}");
            else if (isIOS)
                return Redirect($"youtube://watch?v={videoId}{queryString}");

            return Redirect(webUrl);
        }
        private async Task<IList<YouTubeContent>> FetchMatchesWithoutCache() => (await _shortlinkDataService.FetchMatches())
                            .OrderByDescending(x => x.CreationDateTime)
                            .ToList();


        private async Task<IList<YouTubeContent>> FetchMatches(int skip = 0, int take = int.MaxValue)
        {
            return (await cache.GetOrCreateAsync(CacheKeys.Matches, FetchMatchesWithoutCache)).Skip(skip).Take(take).ToList();
        }
        
        private async Task<IList<ShortLink>> FetchShortlinksWithoutCache() =>
            (await _shortlinkDataService.FetchShortLink())
                        .OrderByDescending(x => x.CreationDateTime)
                        .ToList();
        private Task<IList<ShortLink>> FetchShortlinks() =>
            cache.GetOrCreateAsync(CacheKeys.ShortLinks, FetchShortlinksWithoutCache);

        private async Task<IList<YTChannel>> FetchChannelsWithoutCache() => 
            (await _shortlinkDataService.FetchChannels()).OrderByDescending(x => x.CreationDateTime).ToList());
        private async Task<IList<YTChannel>> FetchChannels()
        {
            return (await cache.GetOrCreateAsync(CacheKeys.Channels,FetchChannelsWithoutCache)).ToList();
        }

        private void ClearCache()
        {
            cache.Remove(CacheKeys.ShortLinks);
            cache.Remove(CacheKeys.Matches);
            cache.Remove(CacheKeys.Channels);
        }
    }
}
