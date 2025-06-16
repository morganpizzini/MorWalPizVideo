using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MorWalPizVideo.Server.Models;
using System.IO;
using System.Windows;

namespace MorWalPiz.VideoImporter.Services
{  /// <summary>
   /// Implementazione del servizio di upload video su YouTube
   /// </summary>
    public class YouTubeUploadService : IYouTubeUploadService
    {
        private YouTubeService _youtubeService;
        private YouTubeService _youtubeUpdateService;
        private readonly string _credentialFilePath;
        private const string AuthStoreName = "YouTube.Upload.Auth.Store";

        /// <summary>
        /// Costruttore del servizio
        /// </summary>
        /// <param name="credentialFilePath">Percorso del file JSON delle credenziali OAuth</param>
        public YouTubeUploadService(string credentialFilePath)
        {
            _credentialFilePath = credentialFilePath;
            InitializeYouTubeService();
        }

        /// <summary>
        /// Inizializza il servizio YouTube con le credenziali OAuth
        /// </summary>
        private void InitializeYouTubeService()
        {
            try
            {
                // Utilizzo di UserCredential invece di GoogleCredential per supportare l'autenticazione OAuth
                using (var stream = new FileStream(_credentialFilePath, FileMode.Open, FileAccess.Read))
                {
                    var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
                    // Ottiene la UserCredential dal file JSON delle credenziali OAuth
                    var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeForceSsl },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(AuthStoreName)).Result;

                    _youtubeService = new YouTubeService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "MorWalPiz Site"
                    });

                    // Ottiene la UserCredential dal file JSON delle credenziali OAuth
                    var credentialUpdate = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        // Aggiunti gli scope necessari per la gestione delle localizzazioni
                        new[] {
                YouTubeService.Scope.YoutubeForceSsl
                        },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(AuthStoreName + ".Update")).Result;

                    _youtubeUpdateService = new YouTubeService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credentialUpdate,
                        ApplicationName = "MorWalPiz Site upload"
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
        /// <returns>Risultati delle operazioni di upload</returns>
        public async Task<IEnumerable<UploadResult>> UploadVideosAsync(IEnumerable<VideoFile> videos,IList<string> tags)
        {
            var results = new List<UploadResult>();

            foreach (var video in videos)
            {
                var result = new UploadResult
                {
                    FileName = video.FileName
                };

                try
                {
                    // Verifica dell'esistenza del file
                    if (!File.Exists(video.FilePath))
                    {
                        result.Success = false;
                        result.ErrorMessage = "Il file non esiste";
                        results.Add(result);
                        continue;
                    }                    // Crea l'oggetto video con i metadati richiesti
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
                            Tags = new List<string> { "mio"}// tags
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
                            switch (progress.Status)
                            {
                                case UploadStatus.Uploading:
                                    Console.WriteLine($"{progress.BytesSent} bytes inviati.");
                                    break;
                                case UploadStatus.Failed:
                                    Console.WriteLine($"Upload fallito: {progress.Exception}");
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
                            result.ErrorMessage = uploadResponse.Exception.Message;
                        }
                        else
                        {
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
                            }
                            catch (Exception ex)
                            {
                                // Log dell'errore ma senza interrompere il flusso principale
                                Console.WriteLine($"Errore nell'aggiornamento delle traduzioni: {ex.Message}");
                                // Imposta un warning nel risultato ma mantieni il successo dell'upload
                                result.WarningMessage = $"Video caricato con successo ma errore nell'aggiornamento delle traduzioni: {ex.Message}";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }

                results.Add(result);
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

                // Reinizializza il servizio dopo la pulizia
                InitializeYouTubeService();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore nella pulizia delle credenziali: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

    }
}
