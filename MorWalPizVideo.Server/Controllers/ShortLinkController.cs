using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MatchType = MorWalPizVideo.Server.Models.MatchType;

namespace MorWalPizVideo.Server.Controllers
{
    [Route("sl")]
    public class ShortLinkController : ApplicationController
    {
        public ShortLinkController(DataService _dataService, IExternalDataService _extDataService, IMorWalPizCache _memoryCache) 
                                    : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet("{videoShortLink}")]
        public async Task<IActionResult> Index(string videoShortLink)
        {
            if (string.IsNullOrWhiteSpace(videoShortLink))
                return BadRequest("Video ID is required.");
            
            if(videoShortLink.ToLower() == "shootingita")
            {
                // Get the User-Agent from the headers
                var userAgent1 = HttpContext.Request.Headers["User-Agent"].ToString();
                
                string videoId1 = "bl0hHkZtCfM";
                // Base fallback URL for web browsers
                string webUrl1 = $"https://www.youtube.com/watch?v={videoId1}";

                // Detect device and set appropriate redirect URL
                if (userAgent1.Contains("Android", StringComparison.OrdinalIgnoreCase))
                    return Redirect($"vnd.youtube://watch?v={videoId1}");
                else if (userAgent1.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                         userAgent1.Contains("iPad", StringComparison.OrdinalIgnoreCase))
                    return Redirect($"youtube://watch?v={videoId1}");

                // Fallback to YouTube web URL for unsupported platforms
                return Redirect(webUrl1);
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

                string videoId;                if (lastMatch.MatchType == MatchType.SingleVideo)
                {
                    videoId = lastMatch.ThumbnailVideoId;
                }
                else
                {
                    var selectedVideo = lastMatch.Videos.OrderByDescending(x => x.CreationDateTime).FirstOrDefault();
                    if (selectedVideo == null)
                        return BadRequest("Video not found");
                    videoId = selectedVideo.YoutubeId;
                }
                  string lastQueryString = "&list=PLS0u4VTR02qSj1w2YktAft_4Vs52O9adG";
                return RedirectYouTubeVideo(videoId, lastQueryString, isAndroid, isIOS);
            }
            
            // Normal shortlink handling
            var shortLink = await dataService.GetShortLink(videoShortLink);
            if (shortLink == null)
                return BadRequest("shortLink not found");

            // Increment click count
            await dataService.UpdateShortlink(shortLink with { ClicksCount = shortLink.ClicksCount + 1 });
                
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
                    return Redirect(playlistWebUrl);                case LinkType.Instagram:
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
                        }                        else if (facebookTarget.Contains("fb.watch"))
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
                        (x.MatchType == MatchType.SingleVideo && x.ThumbnailVideoId == shortLink.Target) || 
                        (x.Videos != null && x.Videos.Any(v => v.YoutubeId == shortLink.Target)));
                    
                    if (existingMatch == null)
                        return BadRequest("Video not found");

                    string videoId = string.Empty;
                    if (existingMatch.MatchType == MatchType.SingleVideo)
                    {
                        videoId = existingMatch.ThumbnailVideoId;
                    }
                    else
                    {
                        var selectedVideo = existingMatch.Videos.FirstOrDefault(x => x.YoutubeId == shortLink.Target);
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
    }
}
