using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    [Route("sl")]
    public class ShortLinkController : ApplicationController
    {
        public ShortLinkController(DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache) 
                                    : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet("{videoShortLink}")]
        public async Task<IActionResult> Index(string videoShortLink)
        {
            if (string.IsNullOrWhiteSpace(videoShortLink))
                return BadRequest("Video ID is required.");

            var shortLinks = await dataService.FetchShortLinks();

            var shortLink = shortLinks.FirstOrDefault(x => x.Code == videoShortLink);

            if (shortLink == null)
                return BadRequest("shortLink not found");

            var existingMatch = (await FetchMatches()).FirstOrDefault(x => (x.IsLink && x.ThumbnailUrl == shortLink.VideoId) || (x.Videos != null && x.Videos.Any(v => v.YoutubeId == shortLink.VideoId)));

            if (existingMatch == null)
                return BadRequest("Video not found");

            var videoId = string.Empty;

            if (existingMatch.IsLink)
            {
                videoId = existingMatch.MatchId;
            }
            else
            {
                var selectedVideo = existingMatch.Videos.FirstOrDefault(x => x.YoutubeId == shortLink.VideoId);
                videoId = selectedVideo?.YoutubeId;
                if (selectedVideo == null)
                    return BadRequest("Video shortLink not found");
            }

            var linkQuerystring = !string.IsNullOrEmpty(shortLink.QueryString) ? $"&{shortLink.QueryString}" : string.Empty;

            await dataService.UpdateShortlink(shortLink with { ClicksCount = shortLink.ClicksCount++ });

            // Get the User-Agent from the headers
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Base fallback URL for web browsers
            string webUrl = $"https://www.youtube.com/watch?v={videoId}{linkQuerystring}";

            // Detect device and set appropriate redirect URL
            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
                return Redirect($"vnd.youtube://watch?v={videoId}{linkQuerystring}");
            else if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                     userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
                return Redirect($"youtube://watch?v={videoId}{linkQuerystring}");

            // Fallback to YouTube web URL for unsupported platforms
            return Redirect(webUrl);
        }

    }
}
