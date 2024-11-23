using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Utils;
namespace MorWalPizVideo.Server.Contracts
{
    public static class ContractUtils
    {
        public static Video Convert(ItemResponse contract) { 
            return new Video(contract.Id,contract.Snippet.Title, contract.Snippet.Description.TrimDescription(), int.Parse(contract.Statistics.ViewCount),
                int.Parse(contract.Statistics.LikeCount),
                int.Parse(contract.Statistics.CommentCount),
                DateOnly.FromDateTime(contract.Snippet.PublishedAt), contract.Snippet.Thumbnails["standard"].Url, contract.ContentDetails.Duration, "");
        }
    }
    public record VideoResponse(IList<ItemResponse> Items)
    {
    }
    public record ItemResponse(string Id, SnippetResponse Snippet, ContentDetailResponse ContentDetails, StatisticsResponse Statistics) 
    {
    }
    public record SnippetResponse(DateTime PublishedAt,string Title,string Description,Dictionary<string, ThumbnailResponse> Thumbnails)
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
