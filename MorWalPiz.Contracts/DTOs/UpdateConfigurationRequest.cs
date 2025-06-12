using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class UpdateConfigurationRequest
  {
    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public object Value { get; set; } = null!;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;
  }
}
