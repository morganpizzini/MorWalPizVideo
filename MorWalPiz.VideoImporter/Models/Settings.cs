using System;

namespace MorWalPiz.VideoImporter.Models
{
  public class Settings
  {
    public int Id { get; set; }
    public string DefaultHashtags { get; set; } = string.Empty;
    public TimeSpan DefaultPublishTime { get; set; } = new TimeSpan(12, 0, 0); // Default alle 12:00
    public string ApiEndpoint { get; set; }= string.Empty; // Endpoint API predefinito
  }
}