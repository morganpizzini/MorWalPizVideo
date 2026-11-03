using System.Text.Json;
using System.Text.RegularExpressions;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.DTOs;

namespace MorWalPizVideo.BackOffice.Services.Ai;

public sealed class LegacyAiWorkflowService(IAiModelGateway modelGateway) : IAiWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiWorkflowResult<IList<ReviewApiVideoResponse>>> ReviewAsync(
        ReviewRequest request,
        string? channelId,
        MorWalPizVideo.Server.Models.ChannelTerminologyConfiguration? terminology,
        CancellationToken cancellationToken)
    {
        var executionId = Guid.NewGuid().ToString("N");
        var languages = ValidateLanguages(request.Languages);
        var requestVideos = request.Videos ?? [];
        var requestNames = request.Names ?? [];
        var videos = requestVideos.Count > 0
            ? requestVideos.Select(video => new ReviewInput(video.Name, video.Context ?? string.Empty)).ToList()
            : requestNames.Select(name => new ReviewInput(name, request.Context ?? string.Empty)).ToList();
        if (videos.Count > 100)
            throw new ArgumentException("A review may contain at most 100 videos.");
        if (videos.Any(video => string.IsNullOrWhiteSpace(video.Name) || video.Name.Length > 500 || video.Context.Length > 4000))
            throw new ArgumentException("Every video name is required and names/contexts must remain within their configured limits.");

        var results = new List<ReviewApiVideoResponse>(videos.Count);
        var terminologyJson = SerializeTerminology(terminology);
        foreach (var group in videos.GroupBy(video => video.Context, StringComparer.Ordinal))
        {
            var items = group.Select(video => new ReviewInputItem(Guid.NewGuid().ToString("N"), video.Name)).ToList();
            for (var offset = 0; offset < items.Count; offset += 10)
            {
                var chunk = items.Skip(offset).Take(10).ToArray();
                var metadata = await CompleteAsync(
                    AiPromptStageCatalog.VideoReviewWorkflow,
                    AiPromptStageKeys.VideoReviewMetadata,
                    new
                    {
                        items = chunk.Select(item => new { item.ItemId, item.Name }),
                        languages,
                        context = group.Key
                    },
                    typeof(Review),
                    raw => JsonSerializer.Deserialize<Review>(raw, JsonOptions) ?? throw InvalidOutput(),
                    cancellationToken);
                EnsureIds(metadata.Videos.Select(item => item.ItemId), chunk.Select(item => item.ItemId));

                TranslatedReview translated = new();
                if (languages.Count > 0)
                {
                    translated = await CompleteAsync(
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
                            terminology = terminologyJson
                        },
                        typeof(TranslatedReview),
                        raw => JsonSerializer.Deserialize<TranslatedReview>(raw, JsonOptions) ?? throw InvalidOutput(),
                        cancellationToken);
                    EnsureIds(translated.Videos.Select(item => item.ItemId), metadata.Videos.Select(item => item.ItemId));
                }

                var translatedById = translated.Videos.ToDictionary(item => item.ItemId, StringComparer.Ordinal);
                foreach (var item in metadata.Videos)
                {
                    translatedById.TryGetValue(item.ItemId, out var translation);
                    var itemTranslations = translation?.Translations.Select(value => new ReviewApiVideoTranslation
                    {
                        Language = value.Language,
                        Title = value.Title,
                        Description = value.Description
                    }).ToList() ?? [];
                    itemTranslations.Add(new ReviewApiVideoTranslation
                    {
                        Language = "IT-it",
                        Title = item.Title,
                        Description = item.Description
                    });
                    results.Add(new ReviewApiVideoResponse
                    {
                        Name = chunk.Single(input => input.ItemId == item.ItemId).Name,
                        ProcessElement = item.ProcessElement,
                        Tags = translation?.Translations.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value.Tags))?.Tags ?? item.Tags,
                        Translations = itemTranslations
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
        var executionId = Guid.NewGuid().ToString("N");
        var languages = ValidateLanguages(request.Languages);
        if (languages.Count == 0)
            return new AiWorkflowResult<List<VideoTranslationResponse>>(executionId, []);

        var translations = await CompleteAsync(
            AiPromptStageCatalog.VideoTranslationWorkflow,
            AiPromptStageKeys.DirectVideoTranslation,
            new { request.Title, request.Description, languages },
            typeof(List<VideoTranslationResponse>),
            raw => JsonSerializer.Deserialize<List<VideoTranslationResponse>>(raw, JsonOptions) ?? throw InvalidOutput(),
            cancellationToken);
        if (translations.Count != languages.Count ||
            !translations.Select(item => item.LanguageCode).ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(languages))
            throw InvalidOutput();
        return new AiWorkflowResult<List<VideoTranslationResponse>>(executionId, translations);
    }

    public async Task<AiWorkflowResult<TranscriptAnalysisResponse>> AnalyzeTranscriptAsync(
        TranscriptAnalysisRequest request,
        string? channelId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Transcript))
            throw new ArgumentException("Transcript is required.", nameof(request));
        if (request.Transcript.Length > 80000)
            throw new ArgumentException("Transcript exceeds the configured input limit.", nameof(request));

        var executionId = Guid.NewGuid().ToString("N");
        var transcript = Regex.Replace(request.Transcript, @"\(\d+:\d+(?::\d+)?\.?\d*\)", string.Empty);
        transcript = string.Join(Environment.NewLine, transcript.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Regex.Replace(line.Trim(), @"\s+", " "))
            .Where(line => line.Length > 0));
        if (transcript.Length == 0)
            throw new ArgumentException("Transcript has no content after timestamp cleanup.", nameof(request));

        var payload = new { transcript, context = request.Context?.Trim() ?? string.Empty };
        var seoDescription = await CompleteAsync(
            AiPromptStageCatalog.TranscriptAnalysisWorkflow,
            AiPromptStageKeys.TranscriptSeoDescription,
            payload,
            null,
            raw => raw.Trim(),
            cancellationToken);
        if (seoDescription.Length is < 250 or > 400)
            throw InvalidOutput();

        var metadata = await CompleteAsync(
            AiPromptStageCatalog.TranscriptAnalysisWorkflow,
            AiPromptStageKeys.TranscriptMetadata,
            new { transcript, seoDescription, context = request.Context?.Trim() ?? string.Empty },
            typeof(TranscriptMetadata),
            raw => JsonSerializer.Deserialize<TranscriptMetadata>(raw, JsonOptions) ?? throw InvalidOutput(),
            cancellationToken);
        if (metadata.Titles.Count is < 3 or > 5 ||
            metadata.Descriptions.Count is < 3 or > 5 ||
            metadata.Hashtags.Count is < 5 or > 8 ||
            metadata.Titles.Any(title => string.IsNullOrWhiteSpace(title) || title.Length > 70) ||
            metadata.Descriptions.Any(description => string.IsNullOrWhiteSpace(description) || description.Length is < 250 or > 400) ||
            metadata.Hashtags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.StartsWith('#')))
            throw InvalidOutput();

        return new AiWorkflowResult<TranscriptAnalysisResponse>(executionId, new TranscriptAnalysisResponse
        {
            SeoDescription = seoDescription,
            Titles = metadata.Titles,
            Descriptions = metadata.Descriptions,
            Hashtags = metadata.Hashtags
        });
    }

    private async Task<T> CompleteAsync<T>(
        string workflowKey,
        string stageKey,
        object payload,
        Type? responseType,
        Func<string, T> parse,
        CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var messages = AiPromptStageCatalog.CreateBaselineMessages(workflowKey, stageKey)
            .Select(message => new AiChatMessage(
                message.Role,
                message.Template.Replace("{{payload}}", payloadJson, StringComparison.Ordinal)))
            .ToArray();
        var output = await modelGateway.CompleteAsync(new AiModelRequest(
            Guid.NewGuid().ToString("N"),
            workflowKey,
            stageKey,
            "legacy-baseline",
            messages,
            responseType), cancellationToken);
        return parse(output);
    }

    private static List<string> ValidateLanguages(IList<string>? requested)
    {
        var languages = requested?.Select(language => language?.Trim() ?? string.Empty).ToList() ?? [];
        if (languages.Count > 10 || languages.Any(language => language.Length is 0 or > 40))
            throw new ArgumentException("Languages must contain at most 10 non-empty values of 40 characters or fewer.");
        if (languages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != languages.Count)
            throw new ArgumentException("Duplicate translation languages are not allowed.");
        return languages;
    }

    private static void EnsureIds(IEnumerable<string> responseIds, IEnumerable<string> expectedIds)
    {
        var actual = responseIds.ToArray();
        var expected = expectedIds.ToHashSet(StringComparer.Ordinal);
        if (actual.Length != expected.Count ||
            actual.Any(string.IsNullOrWhiteSpace) ||
            actual.Distinct(StringComparer.Ordinal).Count() != actual.Length ||
            !actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw InvalidOutput();
    }

    private static string SerializeTerminology(MorWalPizVideo.Server.Models.ChannelTerminologyConfiguration? terminology)
    {
        var configuration = terminology is not null &&
            (terminology.ItalianToEnglish.Count > 0 || terminology.InvariantEnglish.Count > 0)
                ? terminology
                : new MorWalPizVideo.Server.Models.ChannelTerminologyConfiguration
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
        return JsonSerializer.Serialize(new
        {
            italianToEnglish = configuration.ItalianToEnglish.Select(item => new { source = item.Source, target = item.Target }),
            invariantEnglish = configuration.InvariantEnglish.Select(item => new { source = item.Source })
        }, JsonOptions);
    }

    private static InvalidOperationException InvalidOutput()
        => new("The legacy AI provider returned output that does not satisfy the Chat workflow contract.");

    private sealed record ReviewInput(string Name, string Context);
    private sealed record ReviewInputItem(string ItemId, string Name);
}
