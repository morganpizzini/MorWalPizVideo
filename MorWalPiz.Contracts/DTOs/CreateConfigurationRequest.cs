using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
    public class CreateConfigurationRequest
    {
        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public object Value { get; set; } = null!;

        [Required]
        public string Type { get; set; } = string.Empty; // es: "bool", "string", "int", "datetime"

        [Required]
        public string Description { get; set; } = string.Empty;
    }
}
