using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.VideoImporter.Models
{
  public class Language
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } // es. "it", "en", "fr"

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } // es. "Italiano", "English", "Français"

    public bool IsDefault { get; set; }

    public bool IsSelected { get; set; }

    // Relazione con i disclaimer
    public ICollection<Disclaimer> Disclaimers { get; set; }
  }
}