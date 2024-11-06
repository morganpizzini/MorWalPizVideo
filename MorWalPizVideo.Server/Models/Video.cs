namespace MorWalPizVideo.Server.Models
{
    public record Video(string Id,string Title,string Description, string Category = "") : BaseEntity { }
}
