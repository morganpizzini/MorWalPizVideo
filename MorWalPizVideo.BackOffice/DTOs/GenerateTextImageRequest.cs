namespace MorWalPizVideo.BackOffice.DTOs;

public class GenerateTextImageRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string FontStyle { get; set; } = string.Empty;
    public int FontSize { get; set; }
    public string TextColor { get; set; } = "#FFFFFF"; // Bianco di default
    public string OutlineColor { get; set; } = "#000000"; // Nero di default
    public int OutlineThickness { get; set; } = 2; // Spessore traccia di default
}
