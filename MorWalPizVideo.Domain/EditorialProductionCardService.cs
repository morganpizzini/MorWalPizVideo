using MongoDB.Bson;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Domain;

public interface IEditorialProductionCardService
{
    Task<IList<EditorialProductionCard>> GetForChannelAsync(string channelId);
    Task<EditorialProductionCard?> GetAsync(string id, string channelId);
    Task<EditorialProductionCard> CreateAsync(EditorialProductionCard item);
    Task<EditorialProductionCard?> CreateFromPlanAsync(string planId, string channelId, CancellationToken cancellationToken = default);
    Task<EditorialProductionCard?> UpdateAsync(EditorialProductionCard item, string channelId, long expectedRevision);
    Task<bool> DeleteAsync(string id, string channelId);
}

public interface IEditorialProductionCardReferenceValidator
{
    Task ValidateAsync(EditorialProductionCard item, CancellationToken cancellationToken = default);
}

public interface IEditorialProductionCardPlanSource
{
    Task<EditorialProductionCard?> CreateDraftAsync(string planId, string channelId, CancellationToken cancellationToken = default);
}

public sealed class EditorialProductionCardPlanSource(IInsightsService insightsService) : IEditorialProductionCardPlanSource
{
    public async Task<EditorialProductionCard?> CreateDraftAsync(string planId, string channelId, CancellationToken cancellationToken = default)
    {
        var plan = await insightsService.GetContentPlanByIdAsync(planId, channelId);
        if (plan is null)
            return null;

        var newsItems = await insightsService.GetNewsItemsAsync(channelId);
        var sourceUrls = newsItems
            .Where(item => plan.GeneratedFromNewsItemIds.Contains(item.Id, StringComparer.Ordinal))
            .Select(item => item.SourceUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new EditorialProductionCard
        {
            ChannelId = channelId,
            Title = plan.Title,
            Brief = plan.Outline,
            SourceUrls = sourceUrls,
            ContentPlanId = plan.Id,
            TopicId = plan.TopicId,
            Status = EditorialProductionCardStatus.Idea
        };
    }
}

public sealed class EditorialProductionCardReferenceValidator(
    IInsightsService insightsService,
    IYouTubeContentRepository youTubeContentRepository,
    IBlogRepository blogRepository) : IEditorialProductionCardReferenceValidator
{
    public async Task ValidateAsync(EditorialProductionCard item, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(item.TopicId) && await insightsService.GetTopicByIdAsync(item.TopicId, item.ChannelId) is null)
            throw new ArgumentException("The topic does not belong to the selected channel.", nameof(item));

        if (!string.IsNullOrWhiteSpace(item.NewsItemId))
        {
            var newsItem = await insightsService.GetNewsItemByIdAsync(item.NewsItemId, item.ChannelId);
            if (newsItem is null || (!string.IsNullOrWhiteSpace(item.TopicId) && newsItem.TopicId != item.TopicId))
                throw new ArgumentException("The news item does not belong to the selected channel or topic.", nameof(item));
        }

        if (!string.IsNullOrWhiteSpace(item.ContentPlanId))
        {
            var plan = await insightsService.GetContentPlanByIdAsync(item.ContentPlanId, item.ChannelId);
            if (plan is null || (!string.IsNullOrWhiteSpace(item.TopicId) && plan.TopicId != item.TopicId))
                throw new ArgumentException("The content plan does not belong to the selected channel or topic.", nameof(item));
        }

        if (!string.IsNullOrWhiteSpace(item.YoutubeVideoId))
        {
            var video = (await youTubeContentRepository.GetItemsAsync(content =>
                content.OwnerChannelId == item.ChannelId && content.VideoRefs.Any(videoReference => videoReference.YoutubeId == item.YoutubeVideoId)))
                .FirstOrDefault();
            if (video is null)
                throw new ArgumentException("The YouTube video does not belong to the selected channel.", nameof(item));
        }

        if (!string.IsNullOrWhiteSpace(item.BackOfficeContentId) &&
            await blogRepository.GetAsync(item.ChannelId, item.BackOfficeContentId) is null)
            throw new ArgumentException("The BackOffice content does not belong to the selected channel.", nameof(item));
    }
}

public sealed class EditorialProductionCardService(
    IEditorialProductionCardRepository repository,
    IEditorialProductionCardReferenceValidator referenceValidator,
    IEditorialProductionCardPlanSource planSource)
    : IEditorialProductionCardService
{
    private static readonly IReadOnlyDictionary<EditorialProductionCardStatus, IReadOnlySet<EditorialProductionCardStatus>> AllowedTransitions =
        new Dictionary<EditorialProductionCardStatus, IReadOnlySet<EditorialProductionCardStatus>>
        {
            [EditorialProductionCardStatus.Idea] = new HashSet<EditorialProductionCardStatus> { EditorialProductionCardStatus.Idea, EditorialProductionCardStatus.Planned, EditorialProductionCardStatus.Archived },
            [EditorialProductionCardStatus.Planned] = new HashSet<EditorialProductionCardStatus> { EditorialProductionCardStatus.Planned, EditorialProductionCardStatus.InProgress, EditorialProductionCardStatus.Archived },
            [EditorialProductionCardStatus.InProgress] = new HashSet<EditorialProductionCardStatus> { EditorialProductionCardStatus.InProgress, EditorialProductionCardStatus.Ready, EditorialProductionCardStatus.Archived },
            [EditorialProductionCardStatus.Ready] = new HashSet<EditorialProductionCardStatus> { EditorialProductionCardStatus.Ready, EditorialProductionCardStatus.Published, EditorialProductionCardStatus.InProgress, EditorialProductionCardStatus.Archived },
            [EditorialProductionCardStatus.Published] = new HashSet<EditorialProductionCardStatus> { EditorialProductionCardStatus.Published, EditorialProductionCardStatus.Archived },
            [EditorialProductionCardStatus.Archived] = new HashSet<EditorialProductionCardStatus> { EditorialProductionCardStatus.Archived, EditorialProductionCardStatus.Idea }
        };

    public Task<IList<EditorialProductionCard>> GetForChannelAsync(string channelId) =>
        repository.GetItemsAsync(item => item.ChannelId == channelId);

    public async Task<EditorialProductionCard?> GetAsync(string id, string channelId) =>
        (await repository.GetItemsAsync(item => item.Id == id && item.ChannelId == channelId)).FirstOrDefault();

    public async Task<EditorialProductionCard> CreateAsync(EditorialProductionCard item)
    {
        Validate(item);
        await referenceValidator.ValidateAsync(item);
        var now = DateTime.UtcNow;
        var created = Normalize(item) with
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CreationDateTime = now,
            UpdatedDateTime = now,
            Revision = 1
        };
        await repository.AddItemAsync(created);
        return created;
    }

    public async Task<EditorialProductionCard?> CreateFromPlanAsync(string planId, string channelId, CancellationToken cancellationToken = default)
    {
        var draft = await planSource.CreateDraftAsync(planId, channelId, cancellationToken);
        return draft is null ? null : await CreateAsync(draft);
    }

    public async Task<EditorialProductionCard?> UpdateAsync(EditorialProductionCard item, string channelId, long expectedRevision)
    {
        var existing = await GetAsync(item.Id, channelId);
        if (existing is null || existing.Revision != expectedRevision)
            return null;
        if (!AllowedTransitions[existing.Status].Contains(item.Status))
            throw new InvalidOperationException($"Cannot transition a card from {existing.Status} to {item.Status}.");

        Validate(item);
        await referenceValidator.ValidateAsync(item);
        var updated = Normalize(item) with
        {
            Id = existing.Id,
            ChannelId = existing.ChannelId,
            CreationDateTime = existing.CreationDateTime,
            UpdatedDateTime = DateTime.UtcNow,
            Revision = existing.Revision + 1
        };
        await repository.UpdateItemAsync(updated);
        return updated;
    }

    public async Task<bool> DeleteAsync(string id, string channelId)
    {
        var existing = await GetAsync(id, channelId);
        if (existing is null)
            return false;
        await repository.DeleteItemAsync(existing.Id);
        return true;
    }

    private static void Validate(EditorialProductionCard item)
    {
        if (string.IsNullOrWhiteSpace(item.Title) || item.Title.Trim().Length > 200)
            throw new ArgumentException("Title is required and must be at most 200 characters.", nameof(item));
        if (item.EditorialAngle.Length > 2000 || item.Brief.Length > 10000 || item.Script.Length > 50000)
            throw new ArgumentException("Editorial text exceeds the supported length.", nameof(item));
        if (item.SourceUrls.Count > 20 || item.SourceUrls.Any(url => !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
            throw new ArgumentException("Sources must be absolute HTTP or HTTPS URLs.", nameof(item));
        if (!Enum.IsDefined(item.Status))
            throw new ArgumentException("Status is invalid.", nameof(item));
    }

    private static EditorialProductionCard Normalize(EditorialProductionCard item) => item with
    {
        Title = item.Title.Trim(),
        EditorialAngle = item.EditorialAngle.Trim(),
        Brief = item.Brief.Trim(),
        Script = item.Script.Trim(),
        SourceUrls = item.SourceUrls.Select(url => url.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
        DueDateUtc = item.DueDateUtc?.ToUniversalTime()
    };
}