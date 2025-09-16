using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.IO;
using System.Windows;

namespace MorWalPiz.VideoImporter.Services
{  
    /// <summary>
    /// Implementazione del servizio di upload video su YouTube
    /// </summary>
    public class YouTubeUploadService : IYouTubeUploadService
    {
        private YouTubeService _youtubeService;
        private YouTubeService _youtubeUpdateService;
        private string _credentialsJson;
        private string _currentTenantName;
        private const string AuthStoreName = "YouTube.Upload.Auth.Store";

        /// <summary>
        /// Costruttore del servizio
        /// </summary>
        /// <param name="keyVaultService">Servizio per l'accesso ad Azure Key Vault</param>
        /// <param name="tenantName">Nome del tenant iniziale</param>
        public YouTubeUploadService(string credentialsJson, string tenantName)
        {
            _credentialsJson = credentialsJson;
            _currentTenantName = tenantName ?? throw new ArgumentNullException(nameof(tenantName));
            InitializeYouTubeServiceAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Reinizializza il servizio YouTube con nuove credenziali per un tenant
        /// </summary>
        private async Task InitializeYouTubeServiceAsync()
        {
            _credentialsJson = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _currentTenantName = tenantName ?? throw new ArgumentNullException(nameof(tenantName));
            
            // Dispose existing services
            _youtubeService?.Dispose();
            _youtubeUpdateService?.Dispose();
            
            // Reinitialize with new credentials
            await InitializeYouTubeServiceAsync();
        }

        /// <summary>
        /// Inizializza il servizio YouTube con le credenziali OAuth da Key Vault
        /// </summary>
        private async Task InitializeYouTubeServiceAsync()
        {
            using var context = App.DatabaseService.CreateContext();
            var settings = context.Settings.FirstOrDefault();
            if(settings == null)
            {
                System.Windows.MessageBox.Show("Impostazioni non valide. Assicurati di aver configurato l'applicazione correttamente.",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                // Utilizza UserCredential invece di GoogleCredential per supportare l'autenticazione OAuth
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_credentialsJson)))
                {
                    var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
                    // Ottiene la UserCredential dal file JSON delle credenziali OAuth
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeForceSsl },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(AuthStoreName));

                    _youtubeService = new YouTubeService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = settings.ApplicationName
                    });

                    // Ottiene la UserCredential dal file JSON delle credenziali OAuth
                    var credentialUpdate = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        // Aggiunti gli scope necessari per la gestione delle localizzazioni
                        new[] {
                            YouTubeService.Scope.YoutubeForceSsl
                        },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(AuthStoreName + ".Update"));

                    _youtubeUpdateService = new YouTubeService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credentialUpdate,
                        ApplicationName = $"{settings.ApplicationName} upload"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore nell'inizializzazione del servizio YouTube: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifica se le credenziali sono valide
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync()
        {
            try
            {
                if (_youtubeService == null)
                {
                    return false;
                }

                // Prova una semplice chiamata API per verificare se le credenziali sono valide
                var channelsRequest = _youtubeService.Channels.List("snippet");
                channelsRequest.Mine = true;
                var response = await channelsRequest.ExecuteAsync();
                
                return response.Items?.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Inizializza il servizio in modo sincrono (per compatibilità con il costruttore esistente)
        /// </summary>
        private void InitializeYouTubeService()
        {
            // Per compatibilità, manteniamo questo metodo ma lo facciamo chiamare la versione async
            // Nota: questo può ancora causare problemi se chiamato dal thread UI
            Task.Run(async () => await InitializeYouTubeServiceAsync()).Wait();
        }

        /// <summary>
        /// Carica i video selezionati su YouTube
        /// </summary>
        /// <param name="videos">Lista dei video da caricare</param>
        /// <param name="tags">Lista dei tag da applicare</param>
        /// <param name="progressCallback">Callback per il progresso (opzionale)</param>
        /// <returns>Risultati delle operazioni di upload</returns>
        public async Task<IEnumerable<UploadResult>> UploadVideosAsync(IEnumerable<VideoFile> videos, IList<string> tags, Action<UploadProgressInfo> progressCallback = null)
        {
            var results = new List<UploadResult>();
            var videoList = videos.ToList();
            var totalVideos = videoList.Count;
            var currentVideoNumber = 1;

            foreach (var video in videoList)
            {
                var result = new UploadResult
                {
                    FileName = video.FileName
                };

                // Report progress: starting new video
                progressCallback?.Invoke(new UploadProgressInfo
                {
                    CurrentFileName = video.FileName,
                    CurrentVideoNumber = currentVideoNumber,
                    TotalVideos = totalVideos,
                    CurrentVideoProgress = 0,
                    OverallProgress = ((currentVideoNumber - 1) * 100) / totalVideos,
                    Status = $"Iniziando caricamento di {video.FileName}..."
                });

                try
                {
                    // Verifica dell'esistenza del file
                    if (!File.Exists(video.FilePath))
                    {
                        result.Success = false;
                        result.ErrorMessage = "Il file non esiste";
                        results.Add(result);
                        
                        progressCallback?.Invoke(new UploadProgressInfo
                        {
                            CurrentFileName = video.FileName,
                            CurrentVideoNumber = currentVideoNumber,
                            TotalVideos = totalVideos,
                            CurrentVideoProgress = 100,
                            OverallProgress = (currentVideoNumber * 100) / totalVideos,
                            Status = $"Errore: {result.ErrorMessage}"
                        });
                        
                        currentVideoNumber++;
                        continue;
                    }

                    // Crea l'oggetto video con i metadati richiesti
                    var youtubeVideo = new Google.Apis.YouTube.v3.Data.Video
                    {
                        // Titolo dal CleanFileName (originale o modificato)
                        Snippet = new VideoSnippet
                        {
                            Title = video.Title,
                            //!string.IsNullOrEmpty(video.EditedCleanFileName)
                            //    ? video.EditedCleanFileName
                            //    : video.CleanFileName,
                            Description = video.Description,
                            // Categoria standard per contenuti sport/tiro a segno
                            CategoryId = "17", // Sports category
                                               // Imposta la lingua del titolo e della descrizione come italiano
                            DefaultLanguage = video.DefaultLanguage,
                            DefaultAudioLanguage = video.DefaultLanguage,
                            // Aggiungi tag personalizzati
                            Tags = tags
                        },
                        Status = new VideoStatus
                        {
                            // Imposta pubblicazione programmata
                            PublishAtDateTimeOffset = GetPublishDateTime(video),
                            // Contenuto non generato da IA
                            MadeForKids = false,
                            SelfDeclaredMadeForKids = false,
                            // Imposta altered content come 'no'
                            ContainsSyntheticMedia = false,
                            // Imposta monetizzazione
                            License = "creativeCommon",
                            Embeddable = true,
                            PrivacyStatus = "private" // private fino alla data di pubblicazione
                        },
                        RecordingDetails = new VideoRecordingDetails
                        {
                            RecordingDateDateTimeOffset = DateTime.Now,
                        }
                    };

                    // Caricamento del video
                    using (var fileStream = new FileStream(video.FilePath, FileMode.Open))
                    {
                        var videosInsertRequest = _youtubeService.Videos.Insert(
                              youtubeVideo,
                              "snippet,status,recordingDetails",
                              fileStream,
                              "video/*");

                        videosInsertRequest.NotifySubscribers = true;

                        // Eventi di progresso per operazioni di lunga durata
                        videosInsertRequest.ProgressChanged += progress =>
                        {
                            var currentVideoProgress = 0;
                            switch (progress.Status)
                            {
                                case UploadStatus.Starting:
                                    currentVideoProgress = 5;
                                    progressCallback?.Invoke(new UploadProgressInfo
                                    {
                                        CurrentFileName = video.FileName,
                                        CurrentVideoNumber = currentVideoNumber,
                                        TotalVideos = totalVideos,
                                        CurrentVideoProgress = currentVideoProgress,
                                        OverallProgress = ((currentVideoNumber - 1) * 100 + currentVideoProgress) / totalVideos,
                                        Status = "Iniziando upload..."
                                    });
                                    break;
                                case UploadStatus.Uploading:
                                    if (fileStream.Length > 0)
                                    {
                                        currentVideoProgress = (int)((progress.BytesSent * 90) / fileStream.Length) + 5; // 5-95%
                                        progressCallback?.Invoke(new UploadProgressInfo
                                        {
                                            CurrentFileName = video.FileName,
                                            CurrentVideoNumber = currentVideoNumber,
                                            TotalVideos = totalVideos,
                                            CurrentVideoProgress = currentVideoProgress,
                                            OverallProgress = ((currentVideoNumber - 1) * 100 + currentVideoProgress) / totalVideos,
                                            Status = $"Caricamento: {progress.BytesSent:N0} bytes inviati"
                                        });
                                    }
                                    break;
                                case UploadStatus.Failed:
                                    progressCallback?.Invoke(new UploadProgressInfo
                                    {
                                        CurrentFileName = video.FileName,
                                        CurrentVideoNumber = currentVideoNumber,
                                        TotalVideos = totalVideos,
                                        CurrentVideoProgress = 100,
                                        OverallProgress = (currentVideoNumber * 100) / totalVideos,
                                        Status = $"Upload fallito: {progress.Exception?.Message}"
                                    });
                                    break;
                            }
                        };

                        // Caricamento e attesa del completamento
                        var uploadResponse = await videosInsertRequest.UploadAsync();
                        result.YouTubeId = videosInsertRequest.ResponseBody?.Id;
                        result.Success = uploadResponse.Status == UploadStatus.Completed;
                        
                        if (!result.Success)
                        {
                            result.Success = false;
                            result.ErrorMessage = uploadResponse.Exception?.Message ?? "Upload fallito";
                            progressCallback?.Invoke(new UploadProgressInfo
                            {
                                CurrentFileName = video.FileName,
                                CurrentVideoNumber = currentVideoNumber,
                                TotalVideos = totalVideos,
                                CurrentVideoProgress = 100,
                                OverallProgress = (currentVideoNumber * 100) / totalVideos,
                                Status = $"Errore: {result.ErrorMessage}"
                            });
                        }
                        else
                        {
                            // Report progress: processing translations
                            progressCallback?.Invoke(new UploadProgressInfo
                            {
                                CurrentFileName = video.FileName,
                                CurrentVideoNumber = currentVideoNumber,
                                TotalVideos = totalVideos,
                                CurrentVideoProgress = 95,
                                OverallProgress = ((currentVideoNumber - 1) * 100 + 95) / totalVideos,
                                Status = "Elaborazione traduzioni..."
                            });

                            try
                            {
                                // Utilizziamo il servizio con autorizzazioni dedicate per l'aggiornamento
                                var listRequest = _youtubeUpdateService.Videos.List("snippet");
                                listRequest.Id = result.YouTubeId;
                                var listResponse = await listRequest.ExecuteAsync();
                                var uploadedVideo = listResponse.Items.First();

                                uploadedVideo.Localizations = TransformLocalizations(video.Translations);

                                var updateRequest = _youtubeUpdateService.Videos.Update(uploadedVideo, "snippet,localizations");
                                await updateRequest.ExecuteAsync();

                                // Report completion
                                progressCallback?.Invoke(new UploadProgressInfo
                                {
                                    CurrentFileName = video.FileName,
                                    CurrentVideoNumber = currentVideoNumber,
                                    TotalVideos = totalVideos,
                                    CurrentVideoProgress = 100,
                                    OverallProgress = (currentVideoNumber * 100) / totalVideos,
                                    Status = "Completato con successo"
                                });
                            }
                            catch (Exception ex)
                            {
                                // Log dell'errore ma senza interrompere il flusso principale
                                Console.WriteLine($"Errore nell'aggiornamento delle traduzioni: {ex.Message}");
                                // Imposta un warning nel risultato ma mantieni il successo dell'upload
                                result.WarningMessage = $"Video caricato con successo ma errore nell'aggiornamento delle traduzioni: {ex.Message}";
                                
                                progressCallback?.Invoke(new UploadProgressInfo
                                {
                                    CurrentFileName = video.FileName,
                                    CurrentVideoNumber = currentVideoNumber,
                                    TotalVideos = totalVideos,
                                    CurrentVideoProgress = 100,
                                    OverallProgress = (currentVideoNumber * 100) / totalVideos,
                                    Status = "Completato con avvisi"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    
                    progressCallback?.Invoke(new UploadProgressInfo
                    {
                        CurrentFileName = video.FileName,
                        CurrentVideoNumber = currentVideoNumber,
                        TotalVideos = totalVideos,
                        CurrentVideoProgress = 100,
                        OverallProgress = (currentVideoNumber * 100) / totalVideos,
                        Status = $"Errore: {ex.Message}"
                    });
                }

                results.Add(result);
                currentVideoNumber++;
            }

            return results;
        }

        /// <summary>
        /// Ottiene la data e l'ora di pubblicazione combinando la data e l'ora specificate
        /// </summary>
        private DateTime GetPublishDateTime(VideoFile video)
        {
            return video.PublishDate.Add(video.PublishTime);
        }

        /// <summary>
        /// Applica le traduzioni al video caricato
        /// </summary>
        private Dictionary<string, VideoLocalization> TransformLocalizations(Dictionary<int, TranslationItem> translations)
        {

            
            // Dizionario delle lingue disponibili (codice lingua -> nome lingua)
            var languageCodes = new Dictionary<int, string>
                {
                    { 1, "it" },     // Inglese
                    { 2, "en" },     // Inglese
                    { 3, "fr" },     // Francese
                    { 4, "de" },     // Tedesco
                    { 5, "es" },     // Spagnolo
                };

            // Inizializza le localizzazioni se non esistono
            var localizations = new Dictionary<string, VideoLocalization>();

            // Aggiungi traduzioni per ogni lingua disponibile
            foreach (var translation in translations)
            {
                // Verifica che ci sia un titolo tradotto e che esista il codice lingua
                if (string.IsNullOrEmpty(translation.Value.Title) ||
                    !languageCodes.TryGetValue(translation.Key, out string languageCode))
                {
                    continue;
                }

                // Aggiungi la traduzione
                localizations[languageCode] = new VideoLocalization
                {
                    Title = translation.Value.Title,
                    Description = translation.Value.Description ?? string.Empty
                };
            }
            return localizations;
        }

        /// <summary>
        /// Pulisce i dati di autenticazione memorizzati dal FileDataStore
        /// </summary>
        /// <returns>Esito dell'operazione di pulizia</returns>
        public bool ClearStoredCredentials()
        {
            try
            {
                // Posizione standard delle credenziali memorizzate
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string credentialDirPath = Path.Combine(userProfile, ".credentials");

                // Pulisci le credenziali per il servizio principale
                string mainCredentialsPath = Path.Combine(credentialDirPath, AuthStoreName);
                if (Directory.Exists(mainCredentialsPath))
                {
                    Directory.Delete(mainCredentialsPath, true);
                }

                // Pulisci le credenziali per il servizio di aggiornamento
                string updateCredentialsPath = Path.Combine(credentialDirPath, AuthStoreName + ".Update");
                if (Directory.Exists(updateCredentialsPath))
                {
                    Directory.Delete(updateCredentialsPath, true);
                }

                // Reset dei servizi senza reinizializzazione immediata
                _youtubeService = null;
                _youtubeUpdateService = null;
                
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore nella pulizia delle credenziali: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Reinizializza il servizio YouTube in modo asincrono dopo la pulizia delle credenziali
        /// </summary>
        public async Task<bool> ReinitializeServiceAsync()
        {
            try
            {
                await InitializeYouTubeServiceAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore nella reinizializzazione del servizio YouTube: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

    }
}
