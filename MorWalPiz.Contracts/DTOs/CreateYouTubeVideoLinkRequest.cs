namespace MorWalPizVideo.BackOffice.DTOs;

public class CreateYouTubeVideoLinkRequest
{
    public string MatchId { get; set; } = string.Empty;
    public string ContentCreatorName { get; set; } = string.Empty;
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string FontStyle { get; set; } = "Arial";
    public int FontSize { get; set; } = 48;
    public string TextColor { get; set; } = "#FFFFFF";
    public string OutlineColor { get; set; } = "#000000";
    public int OutlineThickness { get; set; } = 2;
}
