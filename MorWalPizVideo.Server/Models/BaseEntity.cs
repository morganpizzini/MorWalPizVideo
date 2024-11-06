namespace MorWalPizVideo.Server.Models
{
    public record BaseEntity
    {
        public DateTime CreationDateTime { get; init; } = DateTime.Now;
    }
}
