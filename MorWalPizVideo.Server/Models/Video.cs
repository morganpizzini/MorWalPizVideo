namespace MorWalPizVideo.Server.Models
{
    public record Video(string Id, string Title, string Description, int Views, int Likes, int Comments, DateOnly PublishedAt, string Thumbnail, string Duration, string Category = "") : BaseEntity
    {
    }
}
