using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Services.Ai;

public sealed record AiWorkflowResult<T>(string ExecutionId, T Value);

public interface IAiWorkflowService
{
    Task<AiWorkflowResult<IList<ReviewApiVideoResponse>>> ReviewAsync(
        ReviewRequest request,
        string? channelId,
        ChannelTerminologyConfiguration? terminology,
        CancellationToken cancellationToken);

    Task<AiWorkflowResult<List<VideoTranslationResponse>>> TranslateAsync(
        VideoTranslationRequest request,
        string? channelId,
        CancellationToken cancellationToken);

    Task<AiWorkflowResult<TranscriptAnalysisResponse>> AnalyzeTranscriptAsync(
        TranscriptAnalysisRequest request,
        string? channelId,
        CancellationToken cancellationToken);
}

public sealed class AiWorkflowService(
    IAiPromptCatalog promptCatalog,
    IAiModelGateway modelGateway,
    IAiOrchestrationRepository repository,
    IOptions<AiOrchestrationOptions> options,
    ILogger<AiWorkflowService> logger) : IAiWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AiOrchestrationOptions settings = options.Value;

    public async Task<AiWorkflowResult<IList<ReviewApiVideoResponse>>> ReviewAsync(
        ReviewRequest request,
        string? channelId,
        ChannelTerminologyConfiguration? terminology,
        CancellationToken cancellationToken)
    {
        var executionId = Guid.NewGuid().ToString("N");
        var languages = ValidateLanguages(request.Languages);
        var requestVideos = request.Videos ?? [];
        var requestNames = request.Names ?? [];
        var sourceVideos = requestVideos.Count > 0
            ? requestVideos.Select(video => new ReviewInput(video.Name, video.Context ?? string.Empty)).ToList()
            : requestNames.Select(name => new ReviewInput(name, request.Context ?? string.Empty)).ToList();
        ValidateReviewInputs(sourceVideos);
        if (sourceVideos.Count == 0)
            return new AiWorkflowResult<IList<ReviewApiVideoResponse>>(executionId, []);

        var results = new List<ReviewApiVideoResponse>(sourceVideos.Count);
        var terminologyText = BuildTerminology(terminology);
        foreach (var contextGroup in sourceVideos.GroupBy(video => video.Context, StringComparer.Ordinal))
        {
            var group = contextGroup.Select(video => new ReviewInputItem(
                Guid.NewGuid().ToString("N"),
                video.Name,
                video.Context)).ToList();

            for (var offset = 0; offset < group.Count; offset += 10)
            {
                var chunk = group.Skip(offset).Take(10).ToList();
                var metadata = await CallStageAsync(
                    AiPromptStageCatalog.VideoReviewWorkflow,
                    AiPromptStageKeys.VideoReviewMetadata,
                    new
                    {
                        items = chunk.Select(item => new { item.ItemId, item.Name }),
                        languages,
                        context = contextGroup.Key
                    },
                    executionId,
                    channelId,
                    chunk.Count,
                    typeof(Review),
                    raw => JsonSerializer.Deserialize<Review>(raw, JsonOptions) ?? throw InvalidOutput(),
                    response => ValidateMetadata(response, chunk),
                    response => response.Videos.Count,
                    cancellationToken);

                TranslatedReview translated;
                if (languages.Count == 0)
                {
                    translated = new TranslatedReview();
                }
                else
                {
                    translated = await CallStageAsync(
                        AiPromptStageCatalog.VideoReviewWorkflow,
                        AiPromptStageKeys.VideoReviewTranslation,
                        new
                        {
                            items = metadata.Videos.Select(video => new
                            {
                                video.ItemId,
                                video.Title,
                                video.Description,
                                video.Tags
                            }),
                            languages,
                            terminology = terminologyText
                        },
                        executionId,
                        channelId,
                        metadata.Videos.Count,
                        typeof(TranslatedReview),
                        raw => JsonSerializer.Deserialize<TranslatedReview>(raw, JsonOptions) ?? throw InvalidOutput(),
                        response => ValidateTranslations(response, metadata.Videos, languages),
                        response => response.Videos.Count,
                        cancellationToken);
                }

                var translationsById = translated.Videos.ToDictionary(video => video.ItemId, StringComparer.Ordinal);
                foreach (var generated in metadata.Videos)
                {
                    var input = chunk.Single(item => item.ItemId == generated.ItemId);
                    translationsById.TryGetValue(generated.ItemId, out var translatedVideo);
                    var translations = translatedVideo?.Translations
                        .Select(item => new ReviewApiVideoTranslation
                        {
                            Language = item.Language,
                            Title = item.Title,
                            Description = item.Description
                        })
                        .ToList() ?? [];
                    translations.Add(new ReviewApiVideoTranslation
                    {
                        Language = "IT-it",
                        Title = generated.Title,
                        Description = generated.Description
                    });

                    results.Add(new ReviewApiVideoResponse
                    {
                        Name = input.Name,
                        ProcessElement = generated.ProcessElement,
                        Tags = translatedVideo?.Translations.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Tags))?.Tags
                            ?? generated.Tags,
                        Translations = translations
                    });
                }
            }
        }

        return new AiWorkflowResult<IList<ReviewApiVideoResponse>>(executionId, results);
    }

    public async Task<AiWorkflowResult<List<VideoTranslationResponse>>> TranslateAsync(
        VideoTranslationRequest request,
        string? channelId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 500)
            throw new ArgumentException("Title is required and must be 500 characters or fewer.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 10000)
            throw new ArgumentException("Description is required and must be 10,000 characters or fewer.", nameof(request));
        var languages = ValidateLanguages(request.Languages);
        var executionId = Guid.NewGuid().ToString("N");
        if (languages.Count == 0)
            return new AiWorkflowResult<List<VideoTranslationResponse>>(executionId, []);

        var translations = await CallStageAsync(
            AiPromptStageCatalog.VideoTranslationWorkflow,
            AiPromptStageKeys.DirectVideoTranslation,
            new { request.Title, request.Description, languages },
            executionId,
            channelId,
            1,
            typeof(List<VideoTranslationResponse>),
            raw => JsonSerializer.Deserialize<List<VideoTranslationResponse>>(raw, JsonOptions) ?? throw InvalidOutput(),
            response => ValidateDirectTranslations(response, languages),
            response => response.Count,
            cancellationToken);
        return new AiWorkflowResult<List<VideoTranslationResponse>>(executionId, translations);
    }

    public async Task<AiWorkflowResult<TranscriptAnalysisResponse>> AnalyzeTranscriptAsync(
        TranscriptAnalysisRequest request,
        string? channelId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Transcript))
            throw new ArgumentException("Transcript is required.", nameof(request));
        if (request.Transcript.Length > settings.MaxWorkflowInputCharacters)
            throw new ArgumentException("Transcript exceeds the configured input limit.", nameof(request));

        var cleanedTranscript = CleanupTranscriptText(request.Transcript);
        if (string.IsNullOrWhiteSpace(cleanedTranscript))
            throw new ArgumentException("Transcript has no content after timestamp cleanup.", nameof(request));

        var executionId = Guid.NewGuid().ToString("N");
        var payload = new { transcript = cleanedTranscript, context = request.Context?.Trim() ?? string.Empty };
        var seoDescription = await CallStageAsync(
            AiPromptStageCatalog.TranscriptAnalysisWorkflow,
            AiPromptStageKeys.TranscriptSeoDescription,
            payload,
            executionId,
            channelId,
            1,
            null,
            raw => raw.Trim(),
            response =>
            {
                if (string.IsNullOrWhiteSpace(response) || response.Length is < 250 or > 400)
                    throw InvalidOutput();
            },
            _ => 1,
            cancellationToken);

        var metadata = await CallStageAsync(
            AiPromptStageCatalog.TranscriptAnalysisWorkflow,
            AiPromptStageKeys.TranscriptMetadata,
            new { transcript = cleanedTranscript, seoDescription, context = request.Context?.Trim() ?? string.Empty },
            executionId,
            channelId,
            1,
            typeof(TranscriptMetadata),
            raw => JsonSerializer.Deserialize<TranscriptMetadata>(raw, JsonOptions) ?? throw InvalidOutput(),
            ValidateTranscriptMetadata,
            response => response.Titles.Count,
            cancellationToken);

        return new AiWorkflowResult<TranscriptAnalysisResponse>(executionId, new TranscriptAnalysisResponse
        {
            SeoDescription = seoDescription,
            Titles = metadata.Titles,
            Descriptions = metadata.Descriptions,
            Hashtags = metadata.Hashtags
        });
    }

    private async Task<T> CallStageAsync<T>(
        string workflowKey,
        string stageKey,
        object payload,
        string executionId,
        string? channelId,
        int inputCount,
        Type? responseType,
        Func<string, T> parse,
        Action<T> validate,
        Func<T, int> outputCount,
        CancellationToken cancellationToken)
    {
        AiResolvedPrompt? prompt = null;
        var started = Stopwatch.GetTimestamp();
        try
        {
            prompt = await promptCatalog.ResolveAsync(
                workflowKey,
                stageKey,
                executionId: executionId,
                cancellationToken: cancellationToken);
            var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
            if (payloadJson.Length > settings.MaxWorkflowInputCharacters)
                throw new ArgumentException("Workflow input exceeds the configured size limit.");
            var messages = promptCatalog.Render(prompt.Revision, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["payload"] = payloadJson
            });
            var output = await modelGateway.CompleteAsync(new AiModelRequest(
                executionId,
                workflowKey,
                stageKey,
                prompt.Revision.Id,
                messages,
                responseType), cancellationToken);
            var parsed = parse(output);
            validate(parsed);
            var duration = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            await RecordExecutionAsync(executionId, workflowKey, stageKey, prompt.Revision.Id, channelId,
                "Succeeded", null, duration, inputCount, outputCount(parsed), cancellationToken);
            logger.LogInformation(
                "AI workflow stage completed. ExecutionId={ExecutionId} Workflow={Workflow} Stage={Stage} RevisionId={RevisionId} DurationMs={DurationMs} InputCount={InputCount} OutputCount={OutputCount}",
                executionId, workflowKey, stageKey, prompt.Revision.Id, duration, inputCount, outputCount(parsed));
            return parsed;
        }
        catch (Exception exception)
        {
            var duration = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if (prompt is not null)
            {
                var errorCode = exception switch
                {
                    TimeoutException => "provider_timeout",
                    OperationCanceledException => "cancelled",
                    JsonException => "invalid_structured_output",
                    _ when exception.Message.Contains("schema", StringComparison.OrdinalIgnoreCase) ||
                           exception.Message.Contains("workflow contract", StringComparison.OrdinalIgnoreCase) ||
                           exception.Message.Contains("item IDs", StringComparison.OrdinalIgnoreCase) => "invalid_structured_output",
                    _ => "provider_or_workflow_error"
                };
                await RecordExecutionAsync(executionId, workflowKey, stageKey, prompt.Revision.Id, channelId,
                    "Failed", errorCode, duration, inputCount, 0, cancellationToken);
                logger.LogWarning(
                    "AI workflow stage failed. ExecutionId={ExecutionId} Workflow={Workflow} Stage={Stage} RevisionId={RevisionId} ErrorCode={ErrorCode} DurationMs={DurationMs}",
                    executionId, workflowKey, stageKey, prompt.Revision.Id, errorCode, duration);
            }
            throw;
        }
    }

    private async Task RecordExecutionAsync(
        string executionId,
        string workflowKey,
        string stageKey,
        string revisionId,
        string? channelId,
        string status,
        string? errorCode,
        long durationMilliseconds,
        int inputCount,
        int outputCount,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await repository.AddExecutionAsync(new AiExecutionRecord
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ExecutionId = executionId,
            WorkflowKey = workflowKey,
            StageKey = stageKey,
            RevisionId = revisionId,
            ChannelId = channelId,
            Status = status,
            ErrorCode = errorCode,
            DurationMilliseconds = durationMilliseconds,
            InputCount = inputCount,
            OutputCount = outputCount,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(Math.Max(1, settings.ExecutionRetentionDays)),
            CreationDateTime = now
        }, cancellationToken);
    }

    private List<string> ValidateLanguages(IList<string>? requested)
    {
        var languages = requested?.Select(language => language?.Trim() ?? string.Empty).ToList() ?? [];
        var maxLanguages = Math.Clamp(settings.MaxLanguages, 1, 100);
        if (languages.Count > maxLanguages || languages.Any(language => language.Length is 0 or > 40))
            throw new ArgumentException($"Languages must contain at most {maxLanguages} non-empty values of 40 characters or fewer.");
        if (languages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != languages.Count)
            throw new ArgumentException("Duplicate translation languages are not allowed.");
        return languages;
    }

    private void ValidateReviewInputs(IReadOnlyCollection<ReviewInput> inputs)
    {
        if (inputs.Count > settings.MaxBatchItems)
            throw new ArgumentException($"A review may contain at most {settings.MaxBatchItems} videos.");
        if (inputs.Any(item => string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 500 || item.Context.Length > 4000))
            throw new ArgumentException("Every video name is required and names/contexts must remain within their configured limits.");
        if (inputs.Sum(item => item.Name.Length + item.Context.Length) > settings.MaxWorkflowInputCharacters)
            throw new ArgumentException("Review input exceeds the configured size limit.");
    }

    private static void ValidateMetadata(Review response, IReadOnlyCollection<ReviewInputItem> input)
    {
        ValidateItemIds(response.Videos.Select(video => video.ItemId), input.Select(item => item.ItemId), "metadata");
        var inputIds = input.Select(item => item.ItemId).ToHashSet(StringComparer.Ordinal);
        foreach (var video in response.Videos)
        {
            if (!inputIds.Contains(video.ItemId) ||
                string.IsNullOrWhiteSpace(video.ProcessElement) ||
                string.IsNullOrWhiteSpace(video.Title) || video.Title.Length > 100 ||
                string.IsNullOrWhiteSpace(video.Description) || video.Description.Length is < 250 or > 400 ||
                string.IsNullOrWhiteSpace(video.Tags) || video.Tags.Length > 300)
                throw InvalidOutput();
            var tags = video.Tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (tags.Length is < 1 or > 5)
                throw InvalidOutput();
        }
    }

    private static void ValidateTranslations(TranslatedReview response, IEnumerable<ReviewDetails1> metadata, IReadOnlyCollection<string> languages)
    {
        ValidateItemIds(response.Videos.Select(video => video.ItemId), metadata.Select(video => video.ItemId), "translation");
        var expectedLanguages = languages.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var video in response.Videos)
        {
            if (string.IsNullOrWhiteSpace(video.Title) || video.Translations.Count != languages.Count)
                throw InvalidOutput();
            var actualLanguages = video.Translations.Select(item => item.Language).ToArray();
            if (actualLanguages.Any(string.IsNullOrWhiteSpace) ||
                actualLanguages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != actualLanguages.Length ||
                !actualLanguages.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(expectedLanguages))
                throw InvalidOutput();
            if (video.Translations.Any(item =>
                    string.IsNullOrWhiteSpace(item.Title) || item.Title.Length > 500 ||
                    string.IsNullOrWhiteSpace(item.Description) || item.Description.Length > 10000 ||
                    item.Tags.Length > 300))
                throw InvalidOutput();
        }
    }

    private static void ValidateDirectTranslations(IReadOnlyCollection<VideoTranslationResponse> response, IReadOnlyCollection<string> languages)
    {
        var actualLanguages = response.Select(item => item.LanguageCode).ToArray();
        if (response.Count != languages.Count ||
            actualLanguages.Any(string.IsNullOrWhiteSpace) ||
            actualLanguages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != actualLanguages.Length ||
            !actualLanguages.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(languages) ||
            response.Any(item => string.IsNullOrWhiteSpace(item.TranslatedTitle) ||
                string.IsNullOrWhiteSpace(item.TranslatedDescription) ||
                item.TranslatedTitle.Length > 500 || item.TranslatedDescription.Length > 10000))
            throw InvalidOutput();
    }

    private static void ValidateTranscriptMetadata(TranscriptMetadata response)
    {
        if (response.Titles.Count is < 3 or > 5 ||
            response.Descriptions.Count is < 3 or > 5 ||
            response.Hashtags.Count is < 5 or > 8 ||
            response.Titles.Any(title => string.IsNullOrWhiteSpace(title) || title.Length > 70) ||
            response.Descriptions.Any(description => string.IsNullOrWhiteSpace(description) || description.Length is < 250 or > 400) ||
            response.Hashtags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.StartsWith('#') || tag.Length > 50))
            throw InvalidOutput();
    }

    private static void ValidateItemIds(IEnumerable<string> responseIds, IEnumerable<string> expectedIds, string stage)
    {
        var actual = responseIds.ToArray();
        var expected = expectedIds.ToHashSet(StringComparer.Ordinal);
        if (actual.Length != expected.Count ||
            actual.Any(string.IsNullOrWhiteSpace) ||
            actual.Distinct(StringComparer.Ordinal).Count() != actual.Length ||
            !actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new InvalidOperationException($"The AI {stage} response has missing, duplicate, or unknown item IDs.");
    }

    private static string BuildTerminology(ChannelTerminologyConfiguration? configured)
    {
        var normalized = ChannelTerminologyNormalizer.Normalize(configured);
        if (!normalized.IsValid)
            throw new InvalidOperationException("The channel terminology configuration is invalid.");
        var source = normalized.Configuration.ItalianToEnglish!.Count > 0 ||
            normalized.Configuration.InvariantEnglish!.Count > 0
                ? normalized.Configuration
                : DefaultTerminology();
        var payload = JsonSerializer.Serialize(new
        {
            italianToEnglish = source.ItalianToEnglish!.Select(item => new { source = item.Source, target = item.Target }),
            invariantEnglish = source.InvariantEnglish!.Select(item => new { source = item.Source })
        }, JsonOptions);
        return $"<TERMINOLOGIA>{payload}</TERMINOLOGIA>";
    }

    private static ChannelTerminologyConfiguration DefaultTerminology()
        => new()
        {
            ItalianToEnglish =
            [
                new() { Source = "Hit factor", Target = "Factor" },
                new() { Source = "match", Target = "competition/match" },
                new() { Source = "Failure to engage", Target = "Failure to Engage" },
                new() { Source = "Topolino", Target = "Mickey Mouse" },
                new() { Source = "Condizione 1/2/3", Target = "Condition 1/2/3" },
                new() { Source = "mano forte/debole", Target = "strong/weak hand" }
            ],
            InvariantEnglish =
            [
                new() { Source = "No Shoot" },
                new() { Source = "A Zone" },
                new() { Source = "Double Alpha" },
                new() { Source = "Charlie" },
                new() { Source = "Double Charlie" }
            ]
        };

    private static string CleanupTranscriptText(string text)
    {
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        var cleanedLines = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;
            var cleaned = Regex.Replace(trimmed, @"\(\d+:\d+(?::\d+)?\.?\d*\)", string.Empty).Trim();
            if (cleaned.Length > 0)
                cleanedLines.Add(Regex.Replace(cleaned, @"\s+", " "));
        }
        return string.Join(Environment.NewLine, cleanedLines);
    }

    private static InvalidOperationException InvalidOutput()
        => new("The AI provider returned output that does not satisfy the workflow contract.");

    private sealed record ReviewInput(string Name, string Context);
    private sealed record ReviewInputItem(string ItemId, string Name, string Context);
}
