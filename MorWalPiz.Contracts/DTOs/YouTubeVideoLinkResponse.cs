namespace MorWalPizVideo.BackOffice.DTOs;

public class YouTubeVideoLinkResponse
{
    public string ContentCreatorName { get; set; } = string.Empty;
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string? ShortLinkCode { get; set; }
    public string? ShortLinkTarget { get; set; }
}
