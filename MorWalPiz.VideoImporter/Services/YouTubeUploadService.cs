using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private static readonly object _logLock = new object();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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
        /// <param name="tenantName">Nome del tenant per cui recuperare le credenziali</param>
        public async Task ReinitializeWithNewCredentialsAsync(string credentials, string tenantName)
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
        /// Inizializza il servizio YouTube con le credenziali OAuth da Key Vault
        /// </summary>
        private async Task InitializeYouTubeServiceAsync()
        {
            using var context = App.DatabaseService.CreateContext();
            var settings = context.Settings.FirstOrDefault();
            if (settings == null)
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
                    // Ottiene la UserCredential dal JSON delle credenziali OAuth
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeForceSsl },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore($"{AuthStoreName}-{_currentTenantName}"));

                    _youtubeService = new YouTubeService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = settings.ApplicationName
                    });

                    // Ottiene la UserCredential dal JSON delle credenziali OAuth per il servizio di aggiornamento
                    var credentialUpdate = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        // Aggiunti gli scope necessari per la gestione delle localizzazioni
                        new[] {
                            YouTubeService.Scope.YoutubeForceSsl
                        },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore($"{AuthStoreName}-{_currentTenantName}.Update"));

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
                            Tags = MergeTags(tags, video.Tags)
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

                    // Log the Video object before sending to API
                    LogApiCall("VideoInsert", video.FileName, youtubeVideo, null, "About to upload video to YouTube");

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
                        
                        // Log the upload response
                        LogApiCall("VideoInsert", video.FileName, null, new { 
                            Success = result.Success, 
                            YouTubeId = result.YouTubeId, 
                            UploadStatus = uploadResponse.Status.ToString(),
                            ResponseBody = videosInsertRequest.ResponseBody
                        }, "Video upload completed");

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
                                
                                // Log the list request
                                LogApiCall("VideoList", video.FileName, new { VideoId = result.YouTubeId, Part = "snippet" }, null, "Fetching uploaded video for localization update");
                                
                                var listResponse = await listRequest.ExecuteAsync();
                                var uploadedVideo = listResponse.Items.First();

                                uploadedVideo.Localizations = TransformLocalizations(video.Translations);

                                // Log the update request with localizations
                                LogApiCall("VideoUpdate", video.FileName, uploadedVideo, null, "Updating video with localizations");
                                
                                var updateRequest = _youtubeUpdateService.Videos.Update(uploadedVideo, "snippet,localizations");
                                await updateRequest.ExecuteAsync();
                                
                                // Log successful update response
                                LogApiCall("VideoUpdate", video.FileName, null, new { Success = true, VideoId = result.YouTubeId }, "Video updated successfully with localizations");

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
        /// Registra una chiamata API in un file di log JSON
        /// </summary>
        private void LogApiCall(string operation, string fileName, object apiObject, object response = null, string additionalInfo = null)
        {
            try
            {
                // Crea la directory dei log se non esiste
                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Nome file log con data corrente
                var logFileName = $"YouTube_API_Log_{_currentTenantName}_{DateTime.Now:yyyy-MM-dd}.json";
                var logFilePath = Path.Combine(logDirectory, logFileName);

                // Crea l'oggetto di log
                var logEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    TenantName = _currentTenantName,
                    FileName = fileName,
                    Operation = operation,
                    ApiObject = apiObject,
                    Response = response,
                    AdditionalInfo = additionalInfo
                };

                // Serializza in JSON
                var logJson = JsonSerializer.Serialize(logEntry, _jsonOptions);

                // Scrittura thread-safe nel file
                lock (_logLock)
                {
                    // Se il file esiste, leggi il contenuto esistente come array
                    List<object> logEntries = new List<object>();
                    if (File.Exists(logFilePath))
                    {
                        try
                        {
                            var existingContent = File.ReadAllText(logFilePath);
                            if (!string.IsNullOrWhiteSpace(existingContent))
                            {
                                logEntries = JsonSerializer.Deserialize<List<object>>(existingContent) ?? new List<object>();
                            }
                        }
                        catch
                        {
                            // Se il file è corrotto, inizia un nuovo array
                            logEntries = new List<object>();
                        }
                    }

                    // Aggiungi la nuova entry
                    logEntries.Add(logEntry);

                    // Scrivi l'intero array nel file
                    var fullJson = JsonSerializer.Serialize(logEntries, _jsonOptions);
                    File.WriteAllText(logFilePath, fullJson);
                }
            }
            catch (Exception ex)
            {
                // Log error to console but don't interrupt the upload process
                Console.WriteLine($"Errore durante il logging API: {ex.Message}");
            }
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
                    { 6, "pt" },     // Portoghese
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
        /// Merges default hashtags with video-specific contextual tags
        /// </summary>
        /// <param name="defaultTags">Default hashtags from settings</param>
        /// <param name="videoTags">Video-specific contextual tags (comma-separated)</param>
        /// <returns>Merged and deduplicated list of tags</returns>
        private List<string> MergeTags(IList<string> defaultTags, string videoTags)
        {
            // Start with default tags
            var allTags = new List<string>(defaultTags ?? new List<string>());

            // Add video-specific tags if present
            if (!string.IsNullOrWhiteSpace(videoTags))
            {
                var videoTagsList = videoTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                             .Select(tag => tag.Trim())
                                             .Where(tag => !string.IsNullOrWhiteSpace(tag));
                allTags.AddRange(videoTagsList);
            }

            // Deduplicate (case-insensitive) and return
            return allTags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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

                // Reinizializza il servizio dopo la pulizia
                InitializeYouTubeServiceAsync().GetAwaiter().GetResult();
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
        /// Forza una nuova autenticazione YouTube
        /// </summary>
        /// <returns>True se l'autenticazione è riuscita</returns>
        public async Task<bool> ForceReauthenticationAsync()
        {
            try
            {
                using var context = App.DatabaseService.CreateContext();
                var settings = context.Settings.FirstOrDefault();
                if (settings == null)
                {
                    return false;
                }

                // Dispose existing services
                _youtubeService?.Dispose();
                _youtubeUpdateService?.Dispose();

                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_credentialsJson)))
                {
                    var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

                    // Force new authentication by using a new FileDataStore path
                    var tempAuthStoreName = $"{AuthStoreName}-{_currentTenantName}-{DateTime.Now:yyyyMMddHHmmss}";

                    // Ottiene la UserCredential dal JSON delle credenziali OAuth
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeForceSsl },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(tempAuthStoreName));

                    _youtubeService = new YouTubeService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = settings.ApplicationName
                    });

                    // Ottiene la UserCredential dal JSON delle credenziali OAuth per il servizio di aggiornamento
                    var credentialUpdate = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { YouTubeService.Scope.YoutubeForceSsl },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore($"{tempAuthStoreName}.Update"));

                    _youtubeUpdateService = new YouTubeService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credentialUpdate,
                        ApplicationName = $"{settings.ApplicationName} upload"
                    });

                    // Test the authentication by making a simple API call
                    var channelsRequest = _youtubeService.Channels.List("snippet");
                    channelsRequest.Mine = true;
                    var channelsResponse = await channelsRequest.ExecuteAsync();

                    // If we get here without exception, authentication was successful
                    // Now move the temp credentials to the permanent location
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string credentialDirPath = Path.Combine(userProfile, ".credentials");

                    string tempMainPath = Path.Combine(credentialDirPath, tempAuthStoreName);
                    string tempUpdatePath = Path.Combine(credentialDirPath, $"{tempAuthStoreName}.Update");

                    string mainCredentialsPath = Path.Combine(credentialDirPath, $"{AuthStoreName}-{_currentTenantName}");
                    string updateCredentialsPath = Path.Combine(credentialDirPath, $"{AuthStoreName}-{_currentTenantName}.Update");

                    // Remove old credentials if they exist
                    if (Directory.Exists(mainCredentialsPath))
                        Directory.Delete(mainCredentialsPath, true);
                    if (Directory.Exists(updateCredentialsPath))
                        Directory.Delete(updateCredentialsPath, true);

                    // Move temp credentials to permanent location
                    if (Directory.Exists(tempMainPath))
                        Directory.Move(tempMainPath, mainCredentialsPath);
                    if (Directory.Exists(tempUpdatePath))
                        Directory.Move(tempUpdatePath, updateCredentialsPath);

                    return true;
                }
            }
            catch (Exception)
            {
                // Clean up any temp credentials on failure
                try
                {
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string credentialDirPath = Path.Combine(userProfile, ".credentials");

                    var tempDirs = Directory.GetDirectories(credentialDirPath, $"{AuthStoreName}-{_currentTenantName}-*");
                    foreach (var tempDir in tempDirs)
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { /* Ignore cleanup errors */ }

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

        /// <summary>
        /// Traduce automaticamente un video YouTube esistente e aggiorna le localizzazioni
        /// </summary>
        /// <param name="youtubeVideoId">ID del video YouTube</param>
        /// <returns>Risultato dell'operazione di traduzione e aggiornamento</returns>
        public async Task<VideoLocalizationUpdateResult> AutoTranslateVideoAsync(string youtubeVideoId)
        {
            var result = new VideoLocalizationUpdateResult
            {
                YouTubeVideoId = youtubeVideoId
            };

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(youtubeVideoId))
                {
                    result.Success = false;
                    result.ErrorMessage = "L'ID del video YouTube non può essere vuoto";
                    return result;
                }

                // Get enabled non-default languages from database
                using var dbContext = App.DatabaseService.CreateContext();
                var enabledLanguages = dbContext.Languages
                    .Where(l => !l.IsDefault && l.IsSelected)
                    .ToList();

                if (enabledLanguages.Count == 0)
                {
                    result.Success = true;
                    result.WarningMessage = "Nessuna lingua abilitata per la traduzione. Operazione completata senza traduzioni.";
                    return result;
                }

                // Fetch video from YouTube
                var listRequest = _youtubeUpdateService.Videos.List("snippet");
                listRequest.Id = youtubeVideoId;

                LogApiCall("VideoList", youtubeVideoId, new { VideoId = youtubeVideoId, Part = "snippet" }, null, 
                    "Fetching video for auto-translation");

                var listResponse = await listRequest.ExecuteAsync();

                if (listResponse.Items == null || listResponse.Items.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Video con ID '{youtubeVideoId}' non trovato su YouTube";
                    return result;
                }

                var video = listResponse.Items.First();
                var originalTitle = video.Snippet.Title;
                var originalDescription = video.Snippet.Description;

                result.OriginalTitle = originalTitle;
                result.OriginalDescription = originalDescription;

                // Get API settings
                using var settings = App.DatabaseService.CreateContext();
                var apiSettings = settings.Settings.FirstOrDefault();
                
                if (apiSettings == null || string.IsNullOrWhiteSpace(apiSettings.ApiEndpoint))
                {
                    result.Success = false;
                    result.ErrorMessage = "Endpoint API non configurato nelle impostazioni";
                    return result;
                }

                // Call translation API
                var apiService = new ApiService(apiSettings.ApiEndpoint, null);
                var translations = await apiService.TranslateVideoContentAsync(
                    originalTitle, 
                    originalDescription, 
                    enabledLanguages);

                if (translations == null || translations.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "Nessuna traduzione ricevuta dall'API";
                    return result;
                }

                // Build localizations dictionary
                var localizations = new Dictionary<string, VideoLocalization>();

                foreach (var translation in translations)
                {
                    var language = enabledLanguages.FirstOrDefault(l => 
                        string.Equals(l.Code, translation.LanguageCode, StringComparison.OrdinalIgnoreCase));

                    if (language != null && !string.IsNullOrWhiteSpace(translation.TranslatedTitle))
                    {
                        localizations[language.Code] = new VideoLocalization
                        {
                            Title = translation.TranslatedTitle,
                            Description = translation.TranslatedDescription ?? string.Empty
                        };
                    }
                }

                result.TranslationsCreated = localizations.Count;

                // Update YouTube video with localizations
                video.Localizations = localizations;

                LogApiCall("VideoUpdate", youtubeVideoId, video, null, 
                    $"Updating video with {localizations.Count} localizations");

                var updateRequest = _youtubeUpdateService.Videos.Update(video, "snippet,localizations");
                await updateRequest.ExecuteAsync();

                LogApiCall("VideoUpdate", youtubeVideoId, null, 
                    new { Success = true, VideoId = youtubeVideoId, TranslationsCount = localizations.Count }, 
                    "Video updated successfully with auto-translations");

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Errore durante la traduzione automatica: {ex.Message}";
                
                // Log the error
                Console.WriteLine($"Errore AutoTranslateVideoAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return result;
        }
    }
}
