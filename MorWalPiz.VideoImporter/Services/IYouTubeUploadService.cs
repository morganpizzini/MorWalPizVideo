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
    Task<IEnumerable<UploadResult>> UploadVideosAsync(IEnumerable<VideoFile> videos, IList<string> tags, Action<UploadProgressInfo> progressCallback = null);
        
    /// <summary>
    /// Pulisce le credenziali memorizzate
    /// </summary>
    /// <returns>True se l'operazione è riuscita</returns>
    bool ClearStoredCredentials();
        
    /// <summary>
    /// Reinizializza il servizio YouTube con nuove credenziali per un tenant
    /// </summary>
    /// <param name="tenantName">Nome del tenant per cui recuperare le credenziali</param>
    Task ReinitializeWithNewCredentialsAsync(string credentials, string tenantName);

    /// <summary>
    /// Forza una nuova autenticazione YouTube
    /// </summary>
    /// <returns>True se l'autenticazione è riuscita</returns>
    Task<bool> ForceReauthenticationAsync();
  }

  /// <summary>
  /// Informazioni sul progresso dell'upload
  /// </summary>
  public class UploadProgressInfo
  {
    /// <summary>
    /// Nome del file corrente in upload
    /// </summary>
    public string CurrentFileName { get; set; }

    /// <summary>
    /// Numero del video corrente (1-based)
    /// </summary>
    public int CurrentVideoNumber { get; set; }

    /// <summary>
    /// Numero totale di video da caricare
    /// </summary>
    public int TotalVideos { get; set; }

    /// <summary>
    /// Percentuale di completamento del video corrente (0-100)
    /// </summary>
    public int CurrentVideoProgress { get; set; }

    /// <summary>
    /// Percentuale di completamento totale (0-100)
    /// </summary>
    public int OverallProgress { get; set; }

    /// <summary>
    /// Stato corrente dell'operazione
    /// </summary>
    public string Status { get; set; }
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
