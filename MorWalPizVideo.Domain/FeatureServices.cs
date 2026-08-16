using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services;

public interface IContentService
{
    Task<IList<YouTubeContent>> GetPublicMatchesForChannelAsync(string channelId, int skip, int take);
    Task<int> CountPublicMatchesForChannelAsync(string channelId);
    Task<IList<YouTubeContent>> GetPublicMatchesForChannelsAsync(IReadOnlyCollection<string> channelIds, bool includePrivate, int skip, int take);
    Task<int> CountPublicMatchesForChannelsAsync(IReadOnlyCollection<string> channelIds, bool includePrivate);
    Task<IList<YouTubeContent>> GetMatchesPageAsync(bool includePrivate, int skip, int take);
    Task<int> CountMatchesAsync(bool includePrivate);
    Task<YouTubeContent?> GetMatchByUrlAsync(string url, bool includePrivate);
    Task<IList<YouTubeContent>> GetMatchesByIdsAsync(IList<string> ids, bool includePrivate);
    Task<IList<YouTubeContent>> GetAllMatchesAsync();
    Task<IList<YouTubeContent>> GetAuthorizedMatchesAsync(string userId, bool isAdmin);
    Task<IList<YouTubeContent>> GetAuthorizedMatchesAsync(string userId, bool isAdmin, string channelId);
    Task<YouTubeContent?> GetMatchByIdAsync(string id);
    Task<YouTubeContent?> FindMatchAsync(string matchId);
    Task<YouTubeContent?> FindAuthorizedMatchAsync(string matchId, string userId, bool isAdmin);
    Task<YouTubeContent?> FindAuthorizedMatchAsync(string matchId, string userId, bool isAdmin, string channelId);
    Task<bool> SaveMatchAsync(YouTubeContent entity);
    Task UpdateMatchAsync(YouTubeContent entity);
    Task DeleteMatchAsync(string id);
    Task<IList<Category>> GetCategoriesAsync(IList<string>? ids = null);
    Task<IList<YTChannel>> GetChannelsAsync();
    Task<YTChannel?> FindChannelAsync(string channelNameOrId);
    Task<YTChannel?> GetChannelByIdAsync(string channelId);
    Task UpdateChannelAsync(YTChannel entity);
}

public sealed class ContentService(
    IYouTubeContentRepository youTubeContentRepository,
    ICategoryRepository categoryRepository,
    IYTChannelRepository ytChannelRepository,
    IUserChannelOwnerRepository userChannelOwnerRepository,
    IShortLinkRepository shortLinkRepository,
    IYouTubeContentIndexedCache? indexedCache = null) : IContentService
{
    public async Task<IList<YouTubeContent>> GetPublicMatchesForChannelAsync(string channelId, int skip, int take)
    {
        var matches = indexedCache is null
            ? await youTubeContentRepository.GetPublicOrderedForChannelAsync(channelId, skip, take)
            : [.. (await indexedCache.GetPublicForChannelAsync(channelId))
                .Skip(Math.Max(0, skip))
                .Take(Math.Clamp(take, 1, 200))];
        return indexedCache is null ? await AddCanonicalShortLinksAsync(matches) : matches;
    }

    public async Task<int> CountPublicMatchesForChannelAsync(string channelId)
        => indexedCache is null
            ? (int)await youTubeContentRepository.CountPublicForChannelAsync(channelId)
            : (await indexedCache.GetPublicForChannelAsync(channelId)).Count;

    public async Task<IList<YouTubeContent>> GetPublicMatchesForChannelsAsync(IReadOnlyCollection<string> channelIds, bool includePrivate, int skip, int take)
    {
        if (channelIds.Count == 0)
            return [];

        if (indexedCache is not null)
        {
            var cachedMatches = await indexedCache.GetGlobalAsync();
            return [.. cachedMatches
                .Where(match =>
                    (includePrivate || !match.IsPrivate) &&
                    match.VideoRefs.Any(video => video.ChannelIds.Any(channelIds.Contains)))
                .OrderByDescending(match => match.CreationDateTime)
                .Skip(Math.Max(0, skip))
                .Take(Math.Clamp(take, 1, 200))];
        }

        var matches = await youTubeContentRepository.GetItemsAsync(match =>
            (includePrivate || !match.IsPrivate) &&
            match.VideoRefs.Any(video => video.ChannelIds.Any(channelIds.Contains)));
        return await AddCanonicalShortLinksAsync([.. matches.OrderByDescending(match => match.CreationDateTime)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))]);
    }

    public async Task<int> CountPublicMatchesForChannelsAsync(IReadOnlyCollection<string> channelIds, bool includePrivate)
    {
        if (channelIds.Count == 0)
            return 0;

        if (indexedCache is not null)
        {
            var cachedMatches = await indexedCache.GetGlobalAsync();
            return cachedMatches.Count(match =>
                (includePrivate || !match.IsPrivate) &&
                match.VideoRefs.Any(video => video.ChannelIds.Any(channelIds.Contains)));
        }

        return (await youTubeContentRepository.GetItemsAsync(match =>
            (includePrivate || !match.IsPrivate) &&
            match.VideoRefs.Any(video => video.ChannelIds.Any(channelIds.Contains)))).Count;
    }

    public async Task<IList<YouTubeContent>> GetMatchesPageAsync(bool includePrivate, int skip, int take)
    {
        if (indexedCache is null)
            return await AddCanonicalShortLinksAsync(
                await youTubeContentRepository.GetPublicOrderedAsync(includePrivate, skip, take));

        var matches = await indexedCache.GetGlobalAsync();
        if (!includePrivate)
            matches = matches.Where(match => !match.IsPrivate).ToList();
        return [.. matches
            .OrderByDescending(match => match.LatestPublishedAt == DateTime.MinValue
                ? match.CalculateLatestPublishedAt()
                : match.LatestPublishedAt)
            .ThenByDescending(match => match.CreationDateTime)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))];
    }

    public async Task<int> CountMatchesAsync(bool includePrivate)
    {
        if (indexedCache is null)
            return (int)await youTubeContentRepository.CountPublicAsync(includePrivate);

        var matches = await indexedCache.GetGlobalAsync();
        return includePrivate ? matches.Count : matches.Count(match => !match.IsPrivate);
    }

    public async Task<YouTubeContent?> GetMatchByUrlAsync(string url, bool includePrivate)
    {
        var match = await youTubeContentRepository.GetByUrlAsync(url, includePrivate);
        return match is null ? null : (await AddCanonicalShortLinksAsync([match])).Single();
    }

    public async Task<IList<YouTubeContent>> GetMatchesByIdsAsync(IList<string> ids, bool includePrivate)
        => await AddCanonicalShortLinksAsync(await youTubeContentRepository.GetByIdsAsync(ids, includePrivate));

    public async Task<IList<YouTubeContent>> GetAllMatchesAsync()
    {
        if (indexedCache is not null)
            return [.. await indexedCache.GetGlobalAsync()];

        return await AddCanonicalShortLinksAsync([.. (await youTubeContentRepository.GetItemsAsync())
            .OrderByDescending(x => x.CreationDateTime)]);
    }

    public async Task<IList<YouTubeContent>> GetAuthorizedMatchesAsync(string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await GetAllMatchesAsync();
        }

        var channelIds = (await userChannelOwnerRepository.GetByUserIdAsync(userId))
            .Where(owner => owner.IsActive)
            .Select(owner => owner.ChannelId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return await AddCanonicalShortLinksAsync([.. (await youTubeContentRepository.GetOwnedAsync(userId, channelIds))
            .OrderByDescending(x => x.CreationDateTime)]);
    }

    public async Task<IList<YouTubeContent>> GetAuthorizedMatchesAsync(string userId, bool isAdmin, string channelId)
    {
        if (isAdmin && indexedCache is not null)
        {
            var cachedMatches = await indexedCache.GetGlobalAsync();
            return [.. cachedMatches.Where(match =>
                    match.OwnerChannelId == channelId ||
                    match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId)))];
        }

        var matches = await youTubeContentRepository.GetItemsAsync(match =>
            match.OwnerChannelId == channelId ||
            match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId)));
        return await AddCanonicalShortLinksAsync([.. matches.OrderByDescending(x => x.CreationDateTime)]);
    }

    public async Task<YouTubeContent?> GetMatchByIdAsync(string id)
        => (await AddCanonicalShortLinksAsync(await youTubeContentRepository.GetItemsAsync(x => x.Id == id))).FirstOrDefault();

    public async Task<YouTubeContent?> FindMatchAsync(string matchId)
    {
        var thumbnailMatch = (await youTubeContentRepository.GetItemsAsync(x => x.ThumbnailVideoId == matchId)).FirstOrDefault();
        if (thumbnailMatch is not null)
        {
            return thumbnailMatch;
        }

        var idMatch = await youTubeContentRepository.GetItemAsync(matchId);
        if (idMatch is not null)
        {
            return idMatch;
        }

        return (await youTubeContentRepository.GetItemsAsync(x => x.VideoRefs != null
            && x.VideoRefs.Where(v => !string.IsNullOrEmpty(v.YoutubeId)).Any(v => v.YoutubeId == matchId))).FirstOrDefault();
    }

    private async Task<IList<YouTubeContent>> AddCanonicalShortLinksAsync(IList<YouTubeContent> matches)
    {
        if (matches.Count == 0)
        {
            return matches;
        }

        var contentIds = matches.Select(match => match.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.Ordinal);
        var canonicalLinks = await shortLinkRepository.GetItemsAsync(link =>
            link.LinkType == LinkType.YouTubeVideo &&
            link.ContentId != null &&
            contentIds.Contains(link.ContentId));

        return matches.Select(match =>
        {
            var linksForMatch = canonicalLinks.Where(link => link.ContentId == match.Id &&
                match.VideoRefs.Any(video => video.YoutubeId == link.Target)).ToList();
            var legacyLinks = match.ShortLinks.Where(link => link.LinkType != LinkType.YouTubeVideo);
            return match with { ShortLinks = [.. legacyLinks, .. linksForMatch] };
        }).ToList();
    }

    public async Task<YouTubeContent?> FindAuthorizedMatchAsync(string matchId, string userId, bool isAdmin)
    {
        var matches = await GetAuthorizedMatchesAsync(userId, isAdmin);
        return matches.FirstOrDefault(match => match.ThumbnailVideoId == matchId ||
            match.Id == matchId || match.VideoRefs.Any(video => video.YoutubeId == matchId));
    }

    public async Task<YouTubeContent?> FindAuthorizedMatchAsync(string matchId, string userId, bool isAdmin, string channelId)
    {
        var matches = await GetAuthorizedMatchesAsync(userId, isAdmin, channelId);
        return matches.FirstOrDefault(match => match.ThumbnailVideoId == matchId ||
            match.Id == matchId || match.VideoRefs.Any(video => video.YoutubeId == matchId));
    }

    public async Task<bool> SaveMatchAsync(YouTubeContent entity)
    {
        var check = await youTubeContentRepository.GetItemsAsync(x => x.Id == entity.Id || x.ThumbnailVideoId == entity.ThumbnailVideoId);
        if (check.Count > 0)
        {
            return false;
        }

        await youTubeContentRepository.AddItemAsync(entity);
        if (indexedCache is not null)
            await indexedCache.NotifyChangedAsync(entity.Id);
        return true;
    }

    public async Task UpdateMatchAsync(YouTubeContent entity)
    {
        var check = await youTubeContentRepository.GetItemsAsync(x => x.Id == entity.Id);
        if (check.Count == 0)
        {
            check = await youTubeContentRepository.GetItemsAsync(x => x.ThumbnailVideoId == entity.ThumbnailVideoId);
        }

        if (check.Count == 0)
        {
            return;
        }

        await youTubeContentRepository.UpdateItemAsync(entity);
        if (indexedCache is not null)
            await indexedCache.NotifyChangedAsync(entity.Id);
    }

    public async Task DeleteMatchAsync(string id)
    {
        await youTubeContentRepository.DeleteItemAsync(id);
        if (indexedCache is not null)
            await indexedCache.NotifyChangedAsync(id);
    }

    public Task<IList<Category>> GetCategoriesAsync(IList<string>? ids = null)
        => categoryRepository.GetItemsAsync(x => ids != null ? ids.Contains(x.Id) : true);

    public Task<IList<YTChannel>> GetChannelsAsync() => ytChannelRepository.GetItemsAsync();

    public async Task<YTChannel?> FindChannelAsync(string channelNameOrId)
        => (await ytChannelRepository.GetItemsAsync(x => x.ChannelName == channelNameOrId)).FirstOrDefault()
            ?? (await ytChannelRepository.GetItemsAsync(x => x.ChannelId == channelNameOrId)).FirstOrDefault();

    public async Task<YTChannel?> GetChannelByIdAsync(string channelId)
        => (await ytChannelRepository.GetItemsAsync(x => x.ChannelId == channelId)).FirstOrDefault();

    public async Task UpdateChannelAsync(YTChannel entity)
    {
        var check = await ytChannelRepository.GetItemsAsync(x => x.ChannelId == entity.ChannelId);
        if (check.Count == 0)
        {
            return;
        }

        await ytChannelRepository.UpdateItemAsync(entity);
    }
}

public interface ICatalogService
{
    Task<IList<Compilation>> GetCompilationsAsync();
    Task<Compilation?> GetCompilationByIdAsync(string id);
    Task<Compilation> SaveCompilationAsync(Compilation entity);
    Task UpdateCompilationAsync(Compilation entity);
    Task DeleteCompilationAsync(string id);
    Task<Page?> GetPageByUrlAsync(string url);
    Task<Compilation?> GetCompilationByUrlAsync(string url);
    Task<IList<CalendarEvent>> GetRecentCalendarEventsAsync(DateTime fromInclusive, int limit);
}

public sealed class CatalogService(
    IPageRepository pageRepository,
    ICompilationRepository compilationRepository,
    ICalendarEventRepository calendarEventRepository) : ICatalogService
{
    public Task<IList<Compilation>> GetCompilationsAsync() => compilationRepository.GetItemsAsync();

    public Task<Compilation?> GetCompilationByIdAsync(string id) => compilationRepository.GetItemAsync(id);

    public async Task<Compilation> SaveCompilationAsync(Compilation entity)
    {
        entity = entity with { Url = Compilation.NormalizeUrl(entity.Url) };
        var existingCompilation = await compilationRepository.GetItemsAsync(x => x.Id == entity.Id);
        if (existingCompilation.Count > 0)
        {
            return entity;
        }

        return await compilationRepository.AddItemAsync(entity);
    }

    public async Task UpdateCompilationAsync(Compilation entity)
    {
        var existingCompilation = await compilationRepository.GetItemsAsync(x => x.Id == entity.Id);
        if (existingCompilation.Count == 0)
        {
            return;
        }

        await compilationRepository.UpdateItemAsync(entity with { Url = Compilation.NormalizeUrl(entity.Url) });
    }

    public async Task DeleteCompilationAsync(string id)
    {
        var compilation = (await compilationRepository.GetItemsAsync(x => x.Id == id)).FirstOrDefault();
        if (compilation == null)
        {
            return;
        }

        await compilationRepository.DeleteItemAsync(compilation.Id);
    }

    public Task<Page?> GetPageByUrlAsync(string url) => pageRepository.GetByUrlAsync(url);

    public Task<Compilation?> GetCompilationByUrlAsync(string url)
        => compilationRepository.GetByUrlAsync(Compilation.NormalizeUrl(url));

    public Task<IList<CalendarEvent>> GetRecentCalendarEventsAsync(DateTime fromInclusive, int limit)
        => calendarEventRepository.GetRecentAsync(fromInclusive, limit);
}

public interface IShopService
{
    Task<IList<Product>> GetProductsAsync(int skip, int take);
}

public sealed class ShopService(IProductRepository productRepository) : IShopService
{
    public Task<IList<Product>> GetProductsAsync(int skip, int take) => productRepository.GetPublicOrderedAsync(skip, take);
}

public interface IShopManagementService
{
    Task<IList<DigitalProduct>> GetDigitalProductsAsync();
    Task<DigitalProduct?> GetDigitalProductByIdAsync(string id);
    Task SaveDigitalProductAsync(DigitalProduct entity);
    Task UpdateDigitalProductAsync(DigitalProduct entity);
    Task DeleteDigitalProductAsync(string id);
    Task<IList<DigitalProductCategory>> GetDigitalProductCategoriesAsync();
    Task<DigitalProductCategory?> GetDigitalProductCategoryByIdAsync(string id);
    Task SaveDigitalProductCategoryAsync(DigitalProductCategory entity);
    Task UpdateDigitalProductCategoryAsync(DigitalProductCategory entity);
    Task DeleteDigitalProductCategoryAsync(string id);
}

public sealed class ShopManagementService(
    IDigitalProductRepository digitalProductRepository,
    IDigitalProductCategoryRepository digitalProductCategoryRepository) : IShopManagementService
{
    public Task<IList<DigitalProduct>> GetDigitalProductsAsync()
        => digitalProductRepository.GetItemsAsync(x => x.IsActive);

    public Task<DigitalProduct?> GetDigitalProductByIdAsync(string id)
        => digitalProductRepository.GetItemAsync(id);

    public async Task SaveDigitalProductAsync(DigitalProduct entity)
    {
        var existingProduct = await digitalProductRepository.GetItemsAsync(x => x.Name.ToLower() == entity.Name.ToLower());
        if (existingProduct.Count > 0)
        {
            return;
        }

        await digitalProductRepository.AddItemAsync(entity);
    }

    public async Task UpdateDigitalProductAsync(DigitalProduct entity)
    {
        var existingProduct = await digitalProductRepository.GetItemsAsync(x => x.Id == entity.Id);
        if (existingProduct.Count == 0)
        {
            return;
        }

        await digitalProductRepository.UpdateItemAsync(entity);
    }

    public async Task DeleteDigitalProductAsync(string id)
    {
        var entity = await digitalProductRepository.GetItemAsync(id);
        if (entity == null)
        {
            return;
        }

        await digitalProductRepository.DeleteItemAsync(entity.Id);
    }

    public Task<IList<DigitalProductCategory>> GetDigitalProductCategoriesAsync()
        => digitalProductCategoryRepository.GetItemsAsync();

    public Task<DigitalProductCategory?> GetDigitalProductCategoryByIdAsync(string id)
        => digitalProductCategoryRepository.GetItemAsync(id);

    public async Task SaveDigitalProductCategoryAsync(DigitalProductCategory entity)
    {
        var existingCategory = await digitalProductCategoryRepository.GetItemsAsync(x => x.Name.ToLower() == entity.Name.ToLower());
        if (existingCategory.Count > 0)
        {
            return;
        }

        await digitalProductCategoryRepository.AddItemAsync(entity);
    }

    public async Task UpdateDigitalProductCategoryAsync(DigitalProductCategory entity)
    {
        var existingCategory = await digitalProductCategoryRepository.GetItemsAsync(x => x.Id == entity.Id);
        if (existingCategory.Count == 0)
        {
            return;
        }

        await digitalProductCategoryRepository.UpdateItemAsync(entity);
    }

    public async Task DeleteDigitalProductCategoryAsync(string id)
    {
        var entity = await digitalProductCategoryRepository.GetItemAsync(id);
        if (entity == null)
        {
            return;
        }

        await digitalProductCategoryRepository.DeleteItemAsync(entity.Id);
    }
}

public sealed record FormResponseCountReconciliation(string FormId, int EmbeddedCount, int CollectionCount)
{
    public bool IsMatch => EmbeddedCount == CollectionCount;
}

public sealed record FormResponseBackfillBatchResult(
    int ProcessedForms,
    int ProcessedResponses,
    int UpsertedResponses,
    string? NextContinuationToken);

public interface IFormsService
{
    Task<IList<CustomForm>> GetAllFormsAsync();
    Task<CustomForm?> GetFormByIdAsync(string id);
    Task SaveFormAsync(CustomForm form);
    Task UpdateFormAsync(CustomForm form);
    Task DeleteFormAsync(string id);
    Task<IList<CustomForm>> GetActiveFormsAsync();
    Task<CustomForm?> GetFormByUrlAsync(string url);
    Task<bool> AddResponseAsync(string formId, CustomFormResponse response);
    Task<IList<CustomFormResponse>> GetResponsesAsync(string formId, int limit = 500);
    Task<int> GetResponseCountAsync(string formId);
    Task<FormResponseCountReconciliation?> ReconcileCountsAsync(string formId);
    Task<FormResponseBackfillBatchResult> BackfillEmbeddedResponsesAsync(string? continuationToken, int batchSize);
}

public sealed class FormsService(
    ICustomFormRepository customFormRepository,
    ICustomFormResponseRepository customFormResponseRepository) : IFormsService
{
    public Task<IList<CustomForm>> GetAllFormsAsync() => customFormRepository.GetItemsAsync();

    public async Task<CustomForm?> GetFormByIdAsync(string id)
        => await customFormRepository.GetItemAsync(id);

    public async Task SaveFormAsync(CustomForm form)
    {
        var existingForm = await customFormRepository.GetItemsAsync(x => x.Title.ToLower() == form.Title.ToLower());
        if (existingForm.Count > 0)
        {
            return;
        }

        await customFormRepository.AddItemAsync(form);
    }

    public async Task UpdateFormAsync(CustomForm form)
    {
        var existingForm = await customFormRepository.GetItemsAsync(x => x.Id == form.Id);
        if (existingForm.Count == 0)
        {
            return;
        }

        await customFormRepository.UpdateItemAsync(form);
    }

    public async Task DeleteFormAsync(string id)
    {
        var form = await customFormRepository.GetItemAsync(id);
        if (form == null)
        {
            return;
        }

        await customFormRepository.DeleteItemAsync(form.Id);
    }

    public Task<IList<CustomForm>> GetActiveFormsAsync() => customFormRepository.GetActiveAsync();

    public Task<CustomForm?> GetFormByUrlAsync(string url) => customFormRepository.GetByUrlAsync(url);

    public async Task<bool> AddResponseAsync(string formId, CustomFormResponse response)
    {
        var form = await customFormRepository.GetItemAsync(formId);
        if (form == null)
        {
            return false;
        }

        var responseDocument = CustomFormResponseDocument.FromResponse(formId, response);
        await customFormResponseRepository.UpsertByFormAndResponseIdAsync(responseDocument);

        var existsInLegacy = form.Responses.Any(x => x.ResponseId == response.ResponseId);
        if (!existsInLegacy)
        {
            var updatedForm = form.AddResponse(response);
            await customFormRepository.UpdateItemAsync(updatedForm);
        }

        return true;
    }

    public async Task<IList<CustomFormResponse>> GetResponsesAsync(string formId, int limit = 500)
    {
        var safeLimit = Math.Clamp(limit, 1, 5000);
        var form = await customFormRepository.GetItemAsync(formId);
        if (form == null)
        {
            return [];
        }

        var responses = new Dictionary<string, CustomFormResponse>(StringComparer.Ordinal);
        var collectionResponses = await customFormResponseRepository.GetByFormIdAsync(formId, safeLimit);
        foreach (var response in collectionResponses.Select(x => x.ToResponse()))
        {
            responses[response.ResponseId] = response;
        }

        return responses.Values
            .OrderByDescending(x => x.SubmittedAt)
            .Take(safeLimit)
            .ToList();
    }

    public Task<int> GetResponseCountAsync(string formId)
        => customFormResponseRepository.CountByFormIdAsync(formId);

    public async Task<FormResponseCountReconciliation?> ReconcileCountsAsync(string formId)
    {
        var form = await customFormRepository.GetItemAsync(formId);
        if (form == null)
        {
            return null;
        }

        var collectionCount = await customFormResponseRepository.CountByFormIdAsync(formId);
        return new FormResponseCountReconciliation(formId, form.Responses.Length, collectionCount);
    }

    public async Task<FormResponseBackfillBatchResult> BackfillEmbeddedResponsesAsync(string? continuationToken, int batchSize)
    {
        var batch = await customFormRepository.GetBatchAsync(continuationToken, batchSize);
        var processedResponses = 0;
        var upsertedResponses = 0;

        foreach (var form in batch)
        {
            foreach (var response in form.Responses)
            {
                processedResponses += 1;
                var inserted = await customFormResponseRepository.UpsertByFormAndResponseIdAsync(
                    CustomFormResponseDocument.FromResponse(form.Id, response));
                if (inserted)
                {
                    upsertedResponses += 1;
                }
            }
        }

        var safeBatchSize = Math.Clamp(batchSize, 1, 200);
        var nextToken = batch.Count == safeBatchSize ? batch[^1].Id : null;
        return new FormResponseBackfillBatchResult(batch.Count, processedResponses, upsertedResponses, nextToken);
    }
}

public interface IInsightsService
{
    Task<IList<InsightTopic>> GetTopicsAsync(string? channelId = null);
    Task<InsightTopic?> GetTopicByIdAsync(string id, string? channelId = null);
    Task SaveTopicAsync(InsightTopic topic, string? channelId = null);
    Task UpdateTopicAsync(InsightTopic topic, string? channelId = null);
    Task DeleteTopicAsync(string id, string? channelId = null);
    Task<IList<InsightNewsItem>> GetNewsItemsAsync(string? channelId = null);
    Task<IList<InsightNewsItem>> GetNewsItemsByTopicIdAsync(string topicId, string? channelId = null);
    Task<InsightNewsItem?> GetNewsItemByIdAsync(string id, string? channelId = null);
    Task SaveNewsItemAsync(InsightNewsItem item, string? channelId = null);
    Task<InsightNewsItem> UpsertYouTubeInsightAsync(InsightNewsItem item, string? channelId = null);
    Task UpdateNewsItemAsync(InsightNewsItem item, string? channelId = null);
    Task DeleteNewsItemAsync(string id, string? channelId = null);
    Task<bool> NewsItemExistsBySourceUrlAsync(string sourceUrl, string? channelId = null);
    Task<IList<InsightContentPlan>> GetContentPlansAsync(string? channelId = null);
    Task<IList<InsightContentPlan>> GetContentPlansByTopicIdAsync(string topicId, string? channelId = null);
    Task<InsightContentPlan?> GetContentPlanByIdAsync(string id, string? channelId = null);
    Task SaveContentPlanAsync(InsightContentPlan plan, string? channelId = null);
    Task UpdateContentPlanAsync(InsightContentPlan plan, string? channelId = null);
    Task DeleteContentPlanAsync(string id, string? channelId = null);
    Task<InsightSourceCursor?> GetSourceCursorAsync(string topicId, string sourceUrl, string? channelId = null);
    Task SaveOrUpdateSourceCursorAsync(InsightSourceCursor cursor, string? channelId = null);
    Task<YTChannel?> GetChannelByNameAsync(string channelName);
    Task<YTChannel?> GetChannelByIdAsync(string channelId);
    Task UpdateChannelAsync(YTChannel channel);
}

public sealed class InsightsService(
    IInsightTopicRepository insightTopicRepository,
    IInsightNewsItemRepository insightNewsItemRepository,
    IInsightContentPlanRepository insightContentPlanRepository,
    IInsightSourceCursorRepository insightSourceCursorRepository,
    IYTChannelRepository ytChannelRepository) : IInsightsService
{
    public static System.Linq.Expressions.Expression<Func<InsightNewsItem, bool>> BuildYouTubeInsightDeduplicationPredicate(
        string topicId,
        string channelId,
        string videoId,
        string sourceUrl) =>
        item => item.TopicId == topicId &&
            item.ChannelId == channelId &&
            (item.VideoId == videoId || (item.VideoId == string.Empty && item.PostId == videoId)) &&
            item.SourceUrl.ToLower() == sourceUrl.ToLower();

    public Task<IList<InsightTopic>> GetTopicsAsync(string? channelId = null) =>
        insightTopicRepository.GetItemsAsync(topic => channelId == null || topic.ChannelId == channelId);

    public async Task<InsightTopic?> GetTopicByIdAsync(string id, string? channelId = null) =>
        (await insightTopicRepository.GetItemsAsync(topic => topic.Id == id && (channelId == null || topic.ChannelId == channelId))).FirstOrDefault();

    public async Task SaveTopicAsync(InsightTopic topic, string? channelId = null)
    {
        topic = channelId == null ? topic : topic with { ChannelId = channelId };
        var existingTopic = await insightTopicRepository.GetItemsAsync(x => x.Title.ToLower() == topic.Title.ToLower());
        if (existingTopic.Count > 0)
        {
            return;
        }

        await insightTopicRepository.AddItemAsync(topic);
    }

    public async Task UpdateTopicAsync(InsightTopic topic, string? channelId = null)
    {
        var existingTopic = await insightTopicRepository.GetItemsAsync(x => x.Id == topic.Id && (channelId == null || x.ChannelId == channelId));
        if (existingTopic.Count == 0)
        {
            return;
        }

        await insightTopicRepository.UpdateItemAsync(channelId == null ? topic : topic with { ChannelId = channelId });
    }

    public async Task DeleteTopicAsync(string id, string? channelId = null)
    {
        var topic = await GetTopicByIdAsync(id, channelId);
        if (topic == null)
        {
            return;
        }

        await insightTopicRepository.DeleteItemAsync(topic.Id);
    }

    public Task<IList<InsightNewsItem>> GetNewsItemsAsync(string? channelId = null) =>
        insightNewsItemRepository.GetItemsAsync(item => channelId == null || item.ChannelId == channelId);

    public Task<IList<InsightNewsItem>> GetNewsItemsByTopicIdAsync(string topicId, string? channelId = null)
        => insightNewsItemRepository.GetItemsAsync(x => x.TopicId == topicId && (channelId == null || x.ChannelId == channelId));

    public async Task<InsightNewsItem?> GetNewsItemByIdAsync(string id, string? channelId = null)
        => (await insightNewsItemRepository.GetItemsAsync(x => x.Id == id && (channelId == null || x.ChannelId == channelId))).FirstOrDefault();

    public async Task SaveNewsItemAsync(InsightNewsItem item, string? channelId = null)
    {
        item = channelId == null ? item : item with { ChannelId = channelId };
        var existing = await insightNewsItemRepository.GetItemsAsync(x => x.SourceUrl.ToLower() == item.SourceUrl.ToLower());
        if (existing.Count > 0)
        {
            return;
        }

        await insightNewsItemRepository.AddItemAsync(item);
    }

    public async Task<InsightNewsItem> UpsertYouTubeInsightAsync(InsightNewsItem item, string? channelId = null)
    {
        item = channelId == null ? item : item with { ChannelId = channelId };
        var videoId = string.IsNullOrWhiteSpace(item.VideoId) ? item.PostId : item.VideoId;
        var existing = await insightNewsItemRepository.GetItemsAsync(BuildYouTubeInsightDeduplicationPredicate(
            item.TopicId, item.ChannelId, videoId, item.SourceUrl));

        var current = existing.FirstOrDefault();
        if (current == null)
        {
            await insightNewsItemRepository.AddItemAsync(item);
            return item;
        }

        var refreshed = item with
        {
            Id = current.Id,
            Status = current.Status,
            StarRating = current.StarRating,
            ReviewReason = current.ReviewReason
        };
        await insightNewsItemRepository.UpdateItemAsync(refreshed);
        return refreshed;
    }

    public async Task UpdateNewsItemAsync(InsightNewsItem item, string? channelId = null)
    {
        var existing = await insightNewsItemRepository.GetItemsAsync(x => x.Id == item.Id && (channelId == null || x.ChannelId == channelId));
        if (existing.Count == 0)
        {
            return;
        }

        await insightNewsItemRepository.UpdateItemAsync(channelId == null ? item : item with { ChannelId = channelId });
    }

    public async Task DeleteNewsItemAsync(string id, string? channelId = null)
    {
        var existing = await GetNewsItemByIdAsync(id, channelId);
        if (existing == null)
        {
            return;
        }

        await insightNewsItemRepository.DeleteItemAsync(existing.Id);
    }

    public async Task<bool> NewsItemExistsBySourceUrlAsync(string sourceUrl, string? channelId = null)
    {
        var existing = await insightNewsItemRepository.GetItemsAsync(x => x.SourceUrl.ToLower() == sourceUrl.ToLower() && (channelId == null || x.ChannelId == channelId));
        return existing.Count > 0;
    }

    public Task<IList<InsightContentPlan>> GetContentPlansAsync(string? channelId = null) =>
        insightContentPlanRepository.GetItemsAsync(plan => channelId == null || plan.ChannelId == channelId);

    public Task<IList<InsightContentPlan>> GetContentPlansByTopicIdAsync(string topicId, string? channelId = null)
        => insightContentPlanRepository.GetItemsAsync(x => x.TopicId == topicId && (channelId == null || x.ChannelId == channelId));

    public async Task<InsightContentPlan?> GetContentPlanByIdAsync(string id, string? channelId = null)
        => (await insightContentPlanRepository.GetItemsAsync(x => x.Id == id && (channelId == null || x.ChannelId == channelId))).FirstOrDefault();

    public async Task SaveContentPlanAsync(InsightContentPlan plan, string? channelId = null)
    {
        plan = channelId == null ? plan : plan with { ChannelId = channelId };
        var existing = await insightContentPlanRepository.GetItemsAsync(x => x.Title.ToLower() == plan.Title.ToLower());
        if (existing.Count > 0)
        {
            return;
        }

        await insightContentPlanRepository.AddItemAsync(plan);
    }

    public async Task UpdateContentPlanAsync(InsightContentPlan plan, string? channelId = null)
    {
        var existing = await insightContentPlanRepository.GetItemsAsync(x => x.Id == plan.Id && (channelId == null || x.ChannelId == channelId));
        if (existing.Count == 0)
        {
            return;
        }

        await insightContentPlanRepository.UpdateItemAsync(channelId == null ? plan : plan with { ChannelId = channelId });
    }

    public async Task DeleteContentPlanAsync(string id, string? channelId = null)
    {
        var existing = await GetContentPlanByIdAsync(id, channelId);
        if (existing == null)
        {
            return;
        }

        await insightContentPlanRepository.DeleteItemAsync(existing.Id);
    }

    public async Task<InsightSourceCursor?> GetSourceCursorAsync(string topicId, string sourceUrl, string? channelId = null)
        => (await insightSourceCursorRepository.GetItemsAsync(x => x.TopicId == topicId && x.SourceUrl == sourceUrl && (channelId == null || x.ChannelId == channelId))).FirstOrDefault();

    public async Task SaveOrUpdateSourceCursorAsync(InsightSourceCursor cursor, string? channelId = null)
    {
        cursor = channelId == null ? cursor : cursor with { ChannelId = channelId };
        var existing = (await insightSourceCursorRepository.GetItemsAsync(x => x.TopicId == cursor.TopicId && x.SourceUrl == cursor.SourceUrl && (channelId == null || x.ChannelId == channelId))).FirstOrDefault();
        if (existing == null)
        {
            await insightSourceCursorRepository.AddItemAsync(cursor);
            return;
        }

        await insightSourceCursorRepository.UpdateItemAsync(cursor with { Id = existing.Id });
    }

    public async Task<YTChannel?> GetChannelByNameAsync(string channelName)
        => (await ytChannelRepository.GetItemsAsync(x => x.ChannelName == channelName)).FirstOrDefault();

    public async Task<YTChannel?> GetChannelByIdAsync(string channelId)
        => (await ytChannelRepository.GetItemsAsync(x => x.ChannelId == channelId)).FirstOrDefault();

    public Task UpdateChannelAsync(YTChannel channel) => ytChannelRepository.UpdateItemAsync(channel);
}

public interface ILinksService
{
    Task<IList<ShortLink>> GetShortLinksAsync();
    Task<ShortLink?> GetByCodeAsync(string code);
    Task<ShortLink?> GetCanonicalVideoShortLinkAsync(string contentId, string youtubeId);
    Task<IList<YouTubeContent>> MergeCanonicalVideoShortLinksAsync(IList<YouTubeContent> matches);
    Task<IList<QueryLink>> GetQueryLinksAsync(IList<string>? ids = null);
    Task<ShortLink> SaveShortLinkAsync(ShortLink entity);
    Task<ShortLink?> EnsureVideoShortLinkAsync(string videoId, string? managementChannelId = null);
    Task<bool> IsCodeAvailableAsync(string code, string? excludingId = null);
    Task UpdateShortLinkAsync(ShortLink entity);
    Task DeleteShortLinkAsync(string shortLinkId);
    Task<int> IncrementClicksAsync(string id);
}

public sealed class LinksService(
    IShortLinkRepository shortLinkRepository,
    IQueryLinkRepository queryLinkRepository,
    IYouTubeContentRepository contentRepository,
    IYTChannelRepository channelRepository) : ILinksService
{
    public Task<IList<ShortLink>> GetShortLinksAsync() => shortLinkRepository.GetItemsAsync();

    public Task<ShortLink?> GetByCodeAsync(string code) => shortLinkRepository.GetByCodeAsync(code);

    public async Task<ShortLink?> GetCanonicalVideoShortLinkAsync(string contentId, string youtubeId)
        => (await shortLinkRepository.GetItemsAsync(link =>
                link.LinkType == LinkType.YouTubeVideo &&
                link.ContentId == contentId &&
                link.Target == youtubeId))
            .OrderBy(link => link.QueryString.Length)
            .FirstOrDefault();

    public async Task<IList<YouTubeContent>> MergeCanonicalVideoShortLinksAsync(IList<YouTubeContent> matches)
    {
        var canonicalLinks = (await shortLinkRepository.GetItemsAsync(link =>
                link.LinkType == LinkType.YouTubeVideo &&
                !string.IsNullOrWhiteSpace(link.ContentId)))
            .GroupBy(link => $"{link.ContentId}\u001f{link.Target}", StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(link => link.QueryString.Length).First(), StringComparer.Ordinal);

        return matches.Select(match =>
        {
            var canonical = match.VideoRefs
                .Select(video => canonicalLinks.GetValueOrDefault($"{match.Id}\u001f{video.YoutubeId}"))
                .Where(link => link is not null)
                .Cast<ShortLink>()
                .ToArray();
            var nonYouTubeLinks = match.ShortLinks
                .Where(link => link.LinkType != LinkType.YouTubeVideo)
                .ToArray();
            return match with { ShortLinks = canonical.Concat(nonYouTubeLinks).ToArray() };
        }).ToList();
    }

    public Task<IList<QueryLink>> GetQueryLinksAsync(IList<string>? ids = null)
        => queryLinkRepository.GetItemsAsync(x => ids != null ? ids.Contains(x.Id) : true);

    public async Task<ShortLink> SaveShortLinkAsync(ShortLink entity)
    {
        var normalizedCode = ShortLink.NormalizeCode(entity.Code);
        var normalizedEntity = entity with { Code = normalizedCode };
        var existingShortLink = await shortLinkRepository.GetItemsAsync(x => x.Code.ToLower() == normalizedCode);
        if (existingShortLink.Count > 0)
        {
            return normalizedEntity;
        }

        return await shortLinkRepository.AddItemAsync(normalizedEntity);
    }

    public async Task<ShortLink?> EnsureVideoShortLinkAsync(string videoId, string? managementChannelId = null)
    {
        var match = (await contentRepository.GetItemsAsync(x =>
                x.ThumbnailVideoId == videoId ||
                x.VideoRefs.Any(video => video.YoutubeId == videoId)))
            .FirstOrDefault();
        if (match is null && MongoDB.Bson.ObjectId.TryParse(videoId, out _))
        {
            match = await contentRepository.GetItemAsync(videoId);
        }

        if (match is null || !match.VideoRefs.Any(video => video.YoutubeId == videoId))
        {
            return null;
        }

        var canonical = await GetCanonicalVideoShortLinkAsync(match.Id, videoId);
        if (canonical is not null)
        {
            await RemoveEmbeddedYouTubeLinksAsync(match);
            return canonical;
        }

        var occupiedCodes = (await shortLinkRepository.GetItemsAsync())
            .Select(link => link.NormalizedCode)
            .Concat((await contentRepository.GetItemsAsync()).SelectMany(content => content.ShortLinks).Select(link => link.NormalizedCode))
            .Concat((await channelRepository.GetItemsAsync()).SelectMany(channel => channel.ShortLinks).Select(link => link.NormalizedCode))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = CreateVideoCode(videoId, attempt, occupiedCodes);
            if (!occupiedCodes.Add(code))
            {
                continue;
            }

            var shortLink = new ShortLink(code, videoId, [])
            {
                LinkType = LinkType.YouTubeVideo,
                ContentId = match.Id,
                ManagementChannelId = managementChannelId
            };
            var persistedLink = await shortLinkRepository.AddItemAsync(shortLink);
            await RemoveEmbeddedYouTubeLinksAsync(match);
            return persistedLink;
        }

        throw new InvalidOperationException("Unable to allocate a unique video shortlink code.");
    }

    private Task RemoveEmbeddedYouTubeLinksAsync(YouTubeContent match)
    {
        var remainingLinks = match.ShortLinks
            .Where(link => link.LinkType != LinkType.YouTubeVideo)
            .ToArray();
        return remainingLinks.Length == match.ShortLinks.Length
            ? Task.CompletedTask
            : contentRepository.UpdateItemAsync(match with { ShortLinks = remainingLinks });
    }

    private static string CreateVideoCode(string videoId, int attempt, ISet<string> occupiedCodes)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var input = System.Text.Encoding.UTF8.GetBytes($"{videoId}:{attempt}");
        var hash = Convert.ToHexString(sha256.ComputeHash(input)).ToLowerInvariant();
        return hash[..5];
    }

    public async Task<bool> IsCodeAvailableAsync(string code, string? excludingId = null)
    {
        var normalizedCode = ShortLink.NormalizeCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return false;
        }

        var standalone = await shortLinkRepository.GetByCodeAsync(normalizedCode);
        if (standalone is not null && !string.Equals(standalone.Id, excludingId, StringComparison.Ordinal))
        {
            return false;
        }

        if ((await contentRepository.GetItemsAsync(match =>
            match.ShortLinks.Any(link => link.MatchesCode(normalizedCode)))).Count > 0)
        {
            return false;
        }

        return !(await channelRepository.GetItemsAsync(channel =>
            channel.ShortLinks.Any(link => link.MatchesCode(normalizedCode)))).Any();
    }

    public async Task UpdateShortLinkAsync(ShortLink entity)
    {
        var normalizedEntity = entity with { Code = ShortLink.NormalizeCode(entity.Code) };
        await shortLinkRepository.UpdateItemAsync(normalizedEntity);
    }

    public async Task DeleteShortLinkAsync(string shortLinkId)
    {
        var shortLink = (await shortLinkRepository.GetItemsAsync(x => x.Id == shortLinkId)).FirstOrDefault();
        if (shortLink == null)
        {
            return;
        }

        await shortLinkRepository.DeleteItemAsync(shortLink.Id);
    }

    public Task<int> IncrementClicksAsync(string id) => shortLinkRepository.IncrementClicksAsync(id);
}
