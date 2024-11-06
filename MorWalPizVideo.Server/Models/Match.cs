namespace MorWalPizVideo.Server.Models
{
    public record Match(string ThumbnailUrl,string Title,string Description,string Url, Video[] Videos,string Category = "",bool isLink = false) : BaseEntity { }
}
