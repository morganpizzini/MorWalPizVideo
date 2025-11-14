using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MorWalPiz.VideoImporter.Models
{
  public class Disclaimer
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public string Text { get; set; }

    // Chiave esterna per la lingua
    public int LanguageId { get; set; }

    [ForeignKey("LanguageId")]
    public Language Language { get; set; }

    // Multi-tenant support
    public int TenantId { get; set; }
  }
}
