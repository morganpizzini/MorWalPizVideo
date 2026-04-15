using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class PublishSocialRequest
  {
    [Required]
    public string Message { get; set; } = string.Empty;
  }
}