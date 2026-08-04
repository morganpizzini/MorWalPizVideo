using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateDigitalProductCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? DisplayOrder { get; set; }
}

public class UpdateDigitalProductCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? DisplayOrder { get; set; }
}
