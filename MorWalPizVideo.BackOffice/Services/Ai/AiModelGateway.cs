using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Configuration;
using MorWalPizVideo.BackOffice.DTOs;

namespace MorWalPizVideo.BackOffice.Services.Ai;

public sealed record AiChatMessage(string Role, string Content);

public sealed record AiModelRequest(
    string ExecutionId,
    string WorkflowKey,
    string StageKey,
    string RevisionId,
    IReadOnlyList<AiChatMessage> Messages,
    Type? ResponseType);

public interface IAiModelGateway
{
    Task<string> CompleteAsync(AiModelRequest request, CancellationToken cancellationToken);
}

public sealed class SemanticKernelAiModelGateway(
    IChatCompletionService completionService,
    Kernel kernel,
    IOptions<AiOrchestrationOptions> options) : IAiModelGateway
{
    public async Task<string> CompleteAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        foreach (var message in request.Messages)
        {
            switch (message.Role)
            {
                case "system":
                    history.AddSystemMessage(message.Content);
                    break;
                case "user":
                    history.AddUserMessage(message.Content);
                    break;
                case "assistant":
                    history.AddAssistantMessage(message.Content);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported AI message role '{message.Role}'.");
            }
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.ModelTimeoutSeconds));

        var settings = new AzureOpenAIPromptExecutionSettings();
        if (request.ResponseType is not null)
            settings.ResponseFormat = request.ResponseType;

        try
        {
            var result = await completionService.GetChatMessageContentAsync(history, settings, kernel, timeout.Token);
            return string.IsNullOrWhiteSpace(result.Content)
                ? throw new InvalidOperationException("The AI provider returned an empty response.")
                : result.Content;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The AI provider request exceeded its configured timeout.");
        }
    }
}

public sealed class DeterministicAiModelGateway : IAiModelGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<string> CompleteAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payloadText = request.Messages.LastOrDefault(message => message.Role == "user")?.Content
            ?? throw new InvalidOperationException("The mock AI request did not contain a user payload.");
        using var payload = JsonDocument.Parse(payloadText);
        var root = payload.RootElement;

        var response = request.StageKey switch
        {
            AiPromptStageKeys.VideoReviewMetadata => CreateReview(root),
            AiPromptStageKeys.VideoReviewTranslation => CreateReviewTranslations(root),
            AiPromptStageKeys.DirectVideoTranslation => CreateDirectTranslations(root),
            AiPromptStageKeys.TranscriptSeoDescription => CreateSeoDescription(root),
            AiPromptStageKeys.TranscriptMetadata => CreateTranscriptMetadata(),
            AiPromptStageKeys.PromptOptimizer => CreatePromptOptimization(root),
            _ => throw new InvalidOperationException($"No deterministic response is registered for stage '{request.StageKey}'.")
        };

        return Task.FromResult(response);
    }

    private static string CreateReview(JsonElement root)
    {
        var videos = root.GetProperty("items").EnumerateArray().Select(item =>
        {
            var name = item.GetProperty("name").GetString() ?? string.Empty;
            return new ReviewDetails1
            {
                ItemId = item.GetProperty("itemId").GetString() ?? string.Empty,
                Name = name,
                ProcessElement = name,
                Title = Limit(name, 100),
                Description = $"A deterministic mock review of {name}. Discover the details, technique, and practical insights in this video about dynamic sport shooting. Follow the stage, notice how focus and movement shape the run, and share your thoughts, questions, and favorite moment in the comments.",
                Tags = "dynamic shooting, IPSC, IDPA"
            };
        }).ToList();
        return JsonSerializer.Serialize(new Review { Videos = videos }, JsonOptions);
    }

    private static string CreateReviewTranslations(JsonElement root)
    {
        var languages = ReadStrings(root, "languages");
        var videos = root.GetProperty("items").EnumerateArray().Select(item =>
        {
            var itemId = item.GetProperty("itemId").GetString() ?? string.Empty;
            var title = item.GetProperty("title").GetString() ?? string.Empty;
            var description = item.GetProperty("description").GetString() ?? string.Empty;
            var tags = item.GetProperty("tags").GetString() ?? string.Empty;
            return new ReviewDetails
            {
                ItemId = itemId,
                Title = title,
                Translations = languages.Select(language => new ReviewTranslationDetail
                {
                    Language = language,
                    Title = $"{title} ({language})",
                    Description = description,
                    Tags = tags
                }).ToList()
            };
        }).ToList();
        return JsonSerializer.Serialize(new TranslatedReview { Videos = videos }, JsonOptions);
    }

    private static string CreateDirectTranslations(JsonElement root)
    {
        var title = root.GetProperty("title").GetString() ?? string.Empty;
        var description = root.GetProperty("description").GetString() ?? string.Empty;
        var response = ReadStrings(root, "languages").Select(language => new VideoTranslationResponse
        {
            LanguageCode = language,
            TranslatedTitle = $"{title} ({language})",
            TranslatedDescription = description
        }).ToList();
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private static string CreateSeoDescription(JsonElement root)
    {
        var transcript = root.GetProperty("transcript").GetString() ?? string.Empty;
        var lead = string.IsNullOrWhiteSpace(transcript) ? "Video transcript analysis" : transcript.Trim();
        return $"This video explores {Limit(lead, 125)}. Follow the key moments, technique, and practical details of the dynamic sport-shooting run. The transcript highlights what happens without adding unsupported claims. Which moment stood out to you? Share your perspective and favorite technique in the comments.";
    }

    private static string CreateTranscriptMetadata()
        => JsonSerializer.Serialize(new TranscriptMetadata
        {
            Titles = ["Dynamic shooting highlights", "IPSC technique in action", "Sport shooting moments"],
            Descriptions =
            [
                "Explore dynamic sport shooting, practical technique, and the details that shape each stage. Follow the movement, decisions, and focus visible in the run without missing the small moments. Which moment stood out to you? Share your perspective and favorite technique in the comments.",
                "A closer look at IPSC action, decision-making, and range technique. Follow the key moments, notice how the stage unfolds, and consider the choices behind each movement. Tell us which part you would approach differently and why in the comments. Share your practical range tips.",
                "Sport shooting combines focus, movement, and precision. This transcript-grounded recap follows the run and its practical details without adding unsupported claims. See which moments matter most, then join the conversation with your own perspective and favorite moment."
            ],
            Hashtags = ["IPSC", "IDPA", "DynamicShooting", "SportShooting", "Range"]
        }, JsonOptions);

    private static string CreatePromptOptimization(JsonElement root)
    {
        var source = root.GetProperty("systemPrompt").GetString() ?? string.Empty;
        var result = new AiPromptOptimizationDraft
        {
            SystemPrompt = source + Environment.NewLine +
                "Validate every output field against the declared response schema and satisfy all supplied deterministic checks.",
            Rationale = "Added explicit schema and evaluation-check instructions based on aggregate reviewed failures."
        };
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private static List<string> ReadStrings(JsonElement root, string propertyName)
        => root.GetProperty(propertyName).EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToList();

    private static string Limit(string value, int length)
        => value.Length <= length ? value : value[..length];
}
