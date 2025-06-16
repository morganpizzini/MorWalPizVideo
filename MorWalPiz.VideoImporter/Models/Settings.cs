using System;

namespace MorWalPiz.VideoImporter.Models
{
  public class Settings
  {
    public int Id { get; set; }
    public string DefaultHashtags { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; }= string.Empty; // Endpoint API predefinito
  }
}
