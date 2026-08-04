using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services;

public interface IContentService
{
    Task<IList<YouTubeContent>> GetMatchesPageAsync(bool includePrivate, int skip, int take);
    Task<int> CountMatchesAsync(bool includePrivate);
    Task<YouTubeContent?> GetMatchByUrlAsync(string url, bool includePrivate);
    Task<IList<YouTubeContent>> GetMatchesByIdsAsync(IList<string> ids, bool includePrivate);
    Task<IList<YouTubeContent>> GetAllMatchesAsync();
    Task<YouTubeContent?> GetMatchByIdAsync(string id);
    Task<YouTubeContent?> FindMatchAsync(string matchId);
    Task SaveMatchAsync(YouTubeContent entity);
    Task UpdateMatchAsync(YouTubeContent entity);
    Task<IList<Category>> GetCategoriesAsync(IList<string>? ids = null);
    Task<IList<YTChannel>> GetChannelsAsync();
    Task<YTChannel?> FindChannelAsync(string channelNameOrId);
    Task<YTChannel?> GetChannelByIdAsync(string channelId);
    Task UpdateChannelAsync(YTChannel entity);
}

public sealed class ContentService(
    IYouTubeContentRepository youTubeContentRepository,
    ICategoryRepository categoryRepository,
    IYTChannelRepository ytChannelRepository) : IContentService
{
    public Task<IList<YouTubeContent>> GetMatchesPageAsync(bool includePrivate, int skip, int take)
        => youTubeContentRepository.GetPublicOrderedAsync(includePrivate, skip, take);

    public async Task<int> CountMatchesAsync(bool includePrivate)
        => (int)await youTubeContentRepository.CountPublicAsync(includePrivate);

    public Task<YouTubeContent?> GetMatchByUrlAsync(string url, bool includePrivate)
        => youTubeContentRepository.GetByUrlAsync(url, includePrivate);

    public Task<IList<YouTubeContent>> GetMatchesByIdsAsync(IList<string> ids, bool includePrivate)
        => youTubeContentRepository.GetByIdsAsync(ids, includePrivate);

    public async Task<IList<YouTubeContent>> GetAllMatchesAsync()
        => [.. (await youTubeContentRepository.GetItemsAsync()).OrderByDescending(x => x.CreationDateTime)];

    public async Task<YouTubeContent?> GetMatchByIdAsync(string id)
        => (await youTubeContentRepository.GetItemsAsync(x => x.Id == id)).FirstOrDefault();

    public async Task<YouTubeContent?> FindMatchAsync(string matchId)
        => (await youTubeContentRepository.GetItemsAsync(x => x.ThumbnailVideoId == matchId)).FirstOrDefault()
            ?? (await youTubeContentRepository.GetItemsAsync(x => x.Id == matchId)).FirstOrDefault()
            ?? (await youTubeContentRepository.GetItemsAsync(x => x.VideoRefs != null
                && x.VideoRefs.Where(v => !string.IsNullOrEmpty(v.YoutubeId)).Any(v => v.YoutubeId == matchId))).FirstOrDefault();

    public async Task SaveMatchAsync(YouTubeContent entity)
    {
        var check = await youTubeContentRepository.GetItemsAsync(x => x.Id == entity.Id || x.ThumbnailVideoId == entity.ThumbnailVideoId);
        if (check.Count > 0)
        {
            return;
        }

        await youTubeContentRepository.AddItemAsync(entity);
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

        await compilationRepository.UpdateItemAsync(entity);
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

    public Task<Compilation?> GetCompilationByUrlAsync(string url) => compilationRepository.GetByUrlAsync(url);

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
    Task<IList<InsightTopic>> GetTopicsAsync();
    Task<InsightTopic?> GetTopicByIdAsync(string id);
    Task SaveTopicAsync(InsightTopic topic);
    Task UpdateTopicAsync(InsightTopic topic);
    Task DeleteTopicAsync(string id);
    Task<IList<InsightNewsItem>> GetNewsItemsAsync();
    Task<IList<InsightNewsItem>> GetNewsItemsByTopicIdAsync(string topicId);
    Task<InsightNewsItem?> GetNewsItemByIdAsync(string id);
    Task SaveNewsItemAsync(InsightNewsItem item);
    Task UpdateNewsItemAsync(InsightNewsItem item);
    Task DeleteNewsItemAsync(string id);
    Task<bool> NewsItemExistsBySourceUrlAsync(string sourceUrl);
    Task<IList<InsightContentPlan>> GetContentPlansAsync();
    Task<IList<InsightContentPlan>> GetContentPlansByTopicIdAsync(string topicId);
    Task<InsightContentPlan?> GetContentPlanByIdAsync(string id);
    Task SaveContentPlanAsync(InsightContentPlan plan);
    Task UpdateContentPlanAsync(InsightContentPlan plan);
    Task DeleteContentPlanAsync(string id);
    Task<InsightSourceCursor?> GetSourceCursorAsync(string topicId, string sourceUrl);
    Task SaveOrUpdateSourceCursorAsync(InsightSourceCursor cursor);
    Task<YTChannel?> GetChannelByNameAsync(string channelName);
    Task UpdateChannelAsync(YTChannel channel);
}

public sealed class InsightsService(
    IInsightTopicRepository insightTopicRepository,
    IInsightNewsItemRepository insightNewsItemRepository,
    IInsightContentPlanRepository insightContentPlanRepository,
    IInsightSourceCursorRepository insightSourceCursorRepository,
    IYTChannelRepository ytChannelRepository) : IInsightsService
{
    public Task<IList<InsightTopic>> GetTopicsAsync() => insightTopicRepository.GetItemsAsync();

    public Task<InsightTopic?> GetTopicByIdAsync(string id) => insightTopicRepository.GetItemAsync(id);

    public async Task SaveTopicAsync(InsightTopic topic)
    {
        var existingTopic = await insightTopicRepository.GetItemsAsync(x => x.Title.ToLower() == topic.Title.ToLower());
        if (existingTopic.Count > 0)
        {
            return;
        }

        await insightTopicRepository.AddItemAsync(topic);
    }

    public async Task UpdateTopicAsync(InsightTopic topic)
    {
        var existingTopic = await insightTopicRepository.GetItemsAsync(x => x.Id == topic.Id);
        if (existingTopic.Count == 0)
        {
            return;
        }

        await insightTopicRepository.UpdateItemAsync(topic);
    }

    public async Task DeleteTopicAsync(string id)
    {
        var topic = await insightTopicRepository.GetItemAsync(id);
        if (topic == null)
        {
            return;
        }

        await insightTopicRepository.DeleteItemAsync(topic.Id);
    }

    public Task<IList<InsightNewsItem>> GetNewsItemsAsync() => insightNewsItemRepository.GetItemsAsync();

    public Task<IList<InsightNewsItem>> GetNewsItemsByTopicIdAsync(string topicId)
        => insightNewsItemRepository.GetItemsAsync(x => x.TopicId == topicId);

    public Task<InsightNewsItem?> GetNewsItemByIdAsync(string id)
        => insightNewsItemRepository.GetItemAsync(id);

    public async Task SaveNewsItemAsync(InsightNewsItem item)
    {
        var existing = await insightNewsItemRepository.GetItemsAsync(x => x.SourceUrl.ToLower() == item.SourceUrl.ToLower());
        if (existing.Count > 0)
        {
            return;
        }

        await insightNewsItemRepository.AddItemAsync(item);
    }

    public async Task UpdateNewsItemAsync(InsightNewsItem item)
    {
        var existing = await insightNewsItemRepository.GetItemsAsync(x => x.Id == item.Id);
        if (existing.Count == 0)
        {
            return;
        }

        await insightNewsItemRepository.UpdateItemAsync(item);
    }

    public async Task DeleteNewsItemAsync(string id)
    {
        var existing = await insightNewsItemRepository.GetItemAsync(id);
        if (existing == null)
        {
            return;
        }

        await insightNewsItemRepository.DeleteItemAsync(existing.Id);
    }

    public async Task<bool> NewsItemExistsBySourceUrlAsync(string sourceUrl)
    {
        var existing = await insightNewsItemRepository.GetItemsAsync(x => x.SourceUrl.ToLower() == sourceUrl.ToLower());
        return existing.Count > 0;
    }

    public Task<IList<InsightContentPlan>> GetContentPlansAsync() => insightContentPlanRepository.GetItemsAsync();

    public Task<IList<InsightContentPlan>> GetContentPlansByTopicIdAsync(string topicId)
        => insightContentPlanRepository.GetItemsAsync(x => x.TopicId == topicId);

    public Task<InsightContentPlan?> GetContentPlanByIdAsync(string id)
        => insightContentPlanRepository.GetItemAsync(id);

    public async Task SaveContentPlanAsync(InsightContentPlan plan)
    {
        var existing = await insightContentPlanRepository.GetItemsAsync(x => x.Title.ToLower() == plan.Title.ToLower());
        if (existing.Count > 0)
        {
            return;
        }

        await insightContentPlanRepository.AddItemAsync(plan);
    }

    public async Task UpdateContentPlanAsync(InsightContentPlan plan)
    {
        var existing = await insightContentPlanRepository.GetItemsAsync(x => x.Id == plan.Id);
        if (existing.Count == 0)
        {
            return;
        }

        await insightContentPlanRepository.UpdateItemAsync(plan);
    }

    public async Task DeleteContentPlanAsync(string id)
    {
        var existing = await insightContentPlanRepository.GetItemAsync(id);
        if (existing == null)
        {
            return;
        }

        await insightContentPlanRepository.DeleteItemAsync(existing.Id);
    }

    public async Task<InsightSourceCursor?> GetSourceCursorAsync(string topicId, string sourceUrl)
        => (await insightSourceCursorRepository.GetItemsAsync(x => x.TopicId == topicId && x.SourceUrl == sourceUrl)).FirstOrDefault();

    public async Task SaveOrUpdateSourceCursorAsync(InsightSourceCursor cursor)
    {
        var existing = (await insightSourceCursorRepository.GetItemsAsync(x => x.TopicId == cursor.TopicId && x.SourceUrl == cursor.SourceUrl)).FirstOrDefault();
        if (existing == null)
        {
            await insightSourceCursorRepository.AddItemAsync(cursor);
            return;
        }

        await insightSourceCursorRepository.UpdateItemAsync(cursor with { Id = existing.Id });
    }

    public async Task<YTChannel?> GetChannelByNameAsync(string channelName)
        => (await ytChannelRepository.GetItemsAsync(x => x.ChannelName == channelName)).FirstOrDefault();

    public Task UpdateChannelAsync(YTChannel channel) => ytChannelRepository.UpdateItemAsync(channel);
}

public interface ILinksService
{
    Task<IList<ShortLink>> GetShortLinksAsync();
    Task<ShortLink?> GetByCodeAsync(string code);
    Task<IList<QueryLink>> GetQueryLinksAsync(IList<string>? ids = null);
    Task<ShortLink> SaveShortLinkAsync(ShortLink entity);
    Task UpdateShortLinkAsync(ShortLink entity);
    Task DeleteShortLinkAsync(string shortLinkId);
    Task<int> IncrementClicksAsync(string id);
}

public sealed class LinksService(IShortLinkRepository shortLinkRepository, IQueryLinkRepository queryLinkRepository) : ILinksService
{
    public Task<IList<ShortLink>> GetShortLinksAsync() => shortLinkRepository.GetItemsAsync();

    public Task<ShortLink?> GetByCodeAsync(string code) => shortLinkRepository.GetByCodeAsync(code);

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
