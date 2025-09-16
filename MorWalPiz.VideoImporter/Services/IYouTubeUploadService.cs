namespace MorWalPiz.VideoImporter.Services
{
  /// <summary>
  /// Servizio per caricare video su YouTube
  /// </summary>
  public interface IYouTubeUploadService
  {
    /// <summary>
    /// Carica i video selezionati su YouTube
    /// </summary>
    /// <param name="videos">Lista dei video da caricare</param>
    /// <param name="tags">Lista dei tag da applicare</param>
    /// <param name="progressCallback">Callback per il progresso (opzionale)</param>
    /// <returns>Task che rappresenta l'operazione asincrona</returns>
    Task<IEnumerable<UploadResult>> UploadVideosAsync(IEnumerable<VideoFile> videos, IList<string> tags);
    bool ClearStoredCredentials();
    Task<bool> ValidateCredentialsAsync();
    Task<bool> ReinitializeServiceAsync();
  }

  /// <summary>
  /// Risultato dell'operazione di upload
  /// </summary>
  public class UploadResult
  {
    /// <summary>
    /// Nome del file
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Identificativo del video su YouTube
    /// </summary>
    public string YouTubeId { get; set; }

    /// <summary>
    /// Esito dell'operazione
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
