namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IImageGenerationService
{
    Task<string> GenerateCreatorImageAsync(string creatorName, string fontStyle = "Arial", int fontSize = 48, string textColor = "#FFFFFF", string outlineColor = "#000000", int outlineThickness = 2);
    Task<Stream?> GetExistingImageAsync(string imageName);
}
