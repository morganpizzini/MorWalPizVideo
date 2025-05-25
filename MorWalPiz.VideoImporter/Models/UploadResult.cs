using System;

namespace MorWalPiz.VideoImporter.Models
{
  /// <summary>
  /// Rappresenta il risultato di un'operazione di upload su YouTube
  /// </summary>
  public class UploadResult
  {
    /// <summary>
    /// Nome del file caricato
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// ID YouTube del video caricato
    /// </summary>
    public string YouTubeId { get; set; }

    /// <summary>
    /// Indica se l'operazione è stata completata con successo
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Messaggio di errore in caso di fallimento
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Messaggio di avviso per operazioni completate con successo ma con avvertimenti
    /// </summary>
    public string WarningMessage { get; set; }
  }
}
