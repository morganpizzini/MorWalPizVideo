using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Utils;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
namespace MorWalPizVideo.Server.Contracts
{
    public static class ContractUtils
    {
        public static Video Convert(ItemResponse contract)
        {
            var snippet = contract.Snippet;
            var statistics = contract.Statistics;
            var contentDetails = contract.ContentDetails;
            var thumbnail = snippet?.Thumbnails?.Values
                .OrderByDescending(item => item.Width)
                .Select(item => item.Url)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url))
                ?? string.Empty;

            return new Video(
                contract.Id ?? string.Empty,
                snippet?.Title ?? string.Empty,
                (snippet?.Description ?? string.Empty).TrimDescription(),
                ParseCount(statistics?.ViewCount),
                ParseCount(statistics?.LikeCount),
                ParseCount(statistics?.CommentCount),
                snippet?.PublishedAt ?? DateTime.MinValue,
                thumbnail,
                contentDetails?.Duration ?? string.Empty,
                Array.Empty<CategoryRef>(),
                snippet?.ChannelId ?? string.Empty);
        }

        private static int ParseCount(string? value)
            => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
    }
    public record SponsorRequest([Required]string Name, [Required][EmailAddress] string Email, [Required][MinLength(10)] string Description, [Required] string Token) { }
    public record RecaptchaResponse(bool success, string action) { }
    public record VideoResponse(IList<ItemResponse> Items)
    {
    }
    public record ItemResponse(string Id, SnippetResponse Snippet, ContentDetailResponse ContentDetails, StatisticsResponse Statistics) 
    {
    }
    public record SnippetResponse(DateTime PublishedAt,string Title,string Description,Dictionary<string, ThumbnailResponse> Thumbnails, string ChannelId = "")
    {
    }
    public record ThumbnailResponse(string Url,int Width,int Height)
    {
    }
    public record ContentDetailResponse(string Duration)
    {
    }
    public record StatisticsResponse(string ViewCount,string LikeCount,string CommentCount)
    {
    }
}
