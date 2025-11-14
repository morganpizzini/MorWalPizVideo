using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.Text;
using System.Text.Json;

namespace MorWalPizVideo.BackOffice.Controllers;

public class ScraperController : ApplicationControllerBase
{
    private readonly DataService dataService;
    private readonly IYTService ytService;
    private readonly Kernel _kernel;

    public ScraperController(IYTService _ytService, DataService _dataService, Kernel kernel)
    {
        ytService = _ytService;
        dataService = _dataService;

        // Configura Semantic Kernel
        _kernel = kernel;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string channelName, int videos = 1, int commentsNumber = 20, bool showVideo = true)
    {
        var channel = await dataService.GetChannel(channelName);
        if (channel == null)
        {
            return BadRequest("Channel not found");
        }

        // Ottieni i video del canale e i relativi commenti
        var (processedVideos, extractedComments) = await ProcessChannelVideos(channel, videos, commentsNumber, showVideo);

        // Salva il canale aggiornato con le informazioni sui video elaborati
        await dataService.UpdateChannel(channel with { Videos = processedVideos });

        // Salva i commenti in un file di testo
        var fileName = $"Comments_{channel.ChannelId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        await System.IO.File.WriteAllTextAsync(filePath, extractedComments);
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, "text/plain", fileName);
    }
    private async Task<(List<YouTubeVideo>, string)> ProcessChannelVideos(YTChannel channel, int videoCount, int commentCount, bool showVideo)
    {
        var processedVideos = channel.Videos?.ToList() ?? new List<YouTubeVideo>();
        var commentBuilder = new StringBuilder();

        // Ottieni i video del canale e i relativi commenti
        var channelComments = await ytService.GetChannelComments(channel.ChannelId, videoCount, commentCount, showVideo);

        foreach (var videoWithComments in channelComments.Videos)
        {
            string videoId = videoWithComments.VideoId;
            string videoTitle = videoWithComments.Title;

            commentBuilder.AppendLine($"Video: {videoTitle} (ID: {videoId})");
            commentBuilder.AppendLine("Comments:");

            // Verifica se il video è già stato elaborato
            var existingVideo = processedVideos.FirstOrDefault(v => v.VideoId == videoId);
            var lastCommentDate = existingVideo?.LastCommentDate ?? DateTime.MinValue;
            var videoIdeas = existingVideo?.VideoIdeas.ToList() ?? new List<VideoIdea>();

            // Raccoglie tutti i commenti da analizzare in batch
            var commentsToAnalyze = new List<(string Author, string Text, DateTime Date)>();
            DateTime newLastCommentDate = lastCommentDate;
            int totalCommentsRetrieved = 0;
            bool foundOldComment = false;

            // Filtra i commenti già analizzati
            foreach (var comment in videoWithComments.Comments)
            {
                if (comment.PublishedAt <= lastCommentDate)
                {
                    foundOldComment = true;
                    continue;
                }

                // Aggiorna la data dell'ultimo commento
                if (comment.PublishedAt > newLastCommentDate)
                {
                    newLastCommentDate = comment.PublishedAt;
                }

                // Aggiungi il commento alla lista di quelli da analizzare
                commentBuilder.AppendLine($"- {comment.Author}: {comment.Text}");
                commentsToAnalyze.Add((comment.Author, comment.Text, comment.PublishedAt));
                totalCommentsRetrieved++;
            }

            // Se abbiamo dei commenti da analizzare, processali in un'unica chiamata
            if (commentsToAnalyze.Count > 0)
            {
                try
                {
                    // Struttura il prompt per l'analisi semantica batch
                    string batchPrompt = $@"
Analizza i seguenti commenti per un video YouTube intitolato '{videoTitle}':

COMMENTI:
{string.Join("\n\n", commentsToAnalyze.Select((c, i) => $"[{i + 1}] Autore: {c.Author}\nCommento: {c.Text}\nData: {c.Date}"))}

Per ogni commento, valuta:
1. Il sentiment generale (positivo, negativo, neutro)
2. Se contiene una richiesta o una domanda per l'autore
3. Se contiene delle osservazioni o un'idea per un potenziale nuovo video che va a coprire il mondo armi, tiro dinamico e affini

Rispondi SOLO con un array JSON contenente oggetti con questa struttura per ciascun commento che suggerisce un'idea per un video:
[
  {{
    ""commentIndex"": numero_indice_commento,
    ""sentiment"": ""positivo/negativo/neutro"",
    ""videoIdea"": ""descrizione dell'idea per il video"",
    ""commentExcerpt"": ""parte del commento che ha ispirato l'idea""
  }},
  ...
]
Se nessun commento contiene idee, restituisci un array vuoto [].
";

                    var result = await _kernel.InvokePromptAsync(batchPrompt);
                    var ideasArray = JsonSerializer.Deserialize<List<BatchCommentAnalysis>>(result.GetValue<string>(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<BatchCommentAnalysis>();

                    // Aggiungi le idee trovate
                    foreach (var idea in ideasArray)
                    {
                        if (!string.IsNullOrEmpty(idea.VideoIdea))
                        {
                            videoIdeas.Add(new VideoIdea
                            {
                                Idea = idea.VideoIdea,
                                CommentExcerpt = idea.CommentExcerpt,
                                CreationDate = DateTime.Now,
                                Sentiment = idea.Sentiment
                            });

                            commentBuilder.AppendLine($"  [IDEA TROVATA: {idea.VideoIdea}]");
                        }
                    }
                }
                catch (Exception ex)
                {
                    commentBuilder.AppendLine($"  [ERRORE ANALISI BATCH: {ex.Message}]");
                }
            }

            commentBuilder.AppendLine($"Total comments retrieved: {totalCommentsRetrieved}");
            commentBuilder.AppendLine();

            // Aggiorna o crea il record del video
            var updatedVideo = new YouTubeVideo
            {
                VideoId = videoId,
                Title = videoTitle,
                LastCommentDate = newLastCommentDate,
                VideoIdeas = videoIdeas
            };

            // Aggiorna la lista dei video
            if (existingVideo != null)
            {
                // Sostituisci il video esistente
                var index = processedVideos.FindIndex(v => v.VideoId == videoId);
                if (index >= 0)
                {
                    processedVideos[index] = updatedVideo;
                }
            }
            else
            {
                // Aggiungi un nuovo video
                processedVideos.Add(updatedVideo);
            }
        }

        return (processedVideos, commentBuilder.ToString());
    }
  
    private class CommentAnalysis
    {
        public string Sentiment { get; set; } = "neutro";
        public bool ContainsRequest { get; set; }
        public bool ContainsVideoIdea { get; set; }
        public string? VideoIdea { get; set; }
        public string? CommentExcerpt { get; set; }
    }

    private class BatchCommentAnalysis
    {
        public int CommentIndex { get; set; }
        public string Sentiment { get; set; } = "neutro";
        public string VideoIdea { get; set; } = string.Empty;
        public string CommentExcerpt { get; set; } = string.Empty;
    }
}
