using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class YouTubeContentRepository : BaseRepository<YouTubeContent>, IYouTubeContentRepository
    {
        public YouTubeContentRepository(IMongoDatabase database) : base(database, DbCollections.YouTubeContent)
        {
        }

        public async Task<IList<VideoPublication>> GetPublicationsAsync(DateTime fromInclusive, DateTime toExclusive, string? channelId = null)
        {
            var filter = Builders<YouTubeContent>.Filter.And(
                    Builders<YouTubeContent>.Filter.Eq(x => x.IsPrivate, false),
                    Builders<YouTubeContent>.Filter.ElemMatch(x => x.VideoRefs,
                        video => video.PublishedAt >= fromInclusive && video.PublishedAt < toExclusive));
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                filter &= Builders<YouTubeContent>.Filter.Or(
                    Builders<YouTubeContent>.Filter.Eq(x => x.OwnerChannelId, channelId),
                    Builders<YouTubeContent>.Filter.ElemMatch(x => x.VideoRefs, video => video.ChannelIds.Contains(channelId)));
            }

            var matches = await _collection.Find(filter)
                .ToListAsync();

            return matches
                .SelectMany(match => match.VideoRefs ?? [])
                .Where(video => video.PublishedAt >= fromInclusive && video.PublishedAt < toExclusive)
                .Select(video => new VideoPublication(video.YoutubeId, video.Title, video.PublishedAt))
                .OrderBy(video => video.PublishedAt)
                .ToList();
        }

        public async Task<IList<YouTubeContent>> GetOwnedAsync(string userId, IList<string> channelIds)
        {
            var filter = Builders<YouTubeContent>.Filter.Eq(x => x.CreatorUserId, userId);
            if (channelIds.Count > 0)
            {
                filter |= Builders<YouTubeContent>.Filter.ElemMatch(
                    x => x.VideoRefs,
                    video => video.ChannelIds.Any(channelIds.Contains));
            }

            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<IList<YouTubeContent>> GetPublicOrderedAsync(bool includePrivate, int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 200);
            var filter = includePrivate
                ? Builders<YouTubeContent>.Filter.Empty
                : Builders<YouTubeContent>.Filter.Eq(x => x.IsPrivate, false);

            return await _collection
                .Find(filter)
                .SortByDescending(x => x.CreationDateTime)
                .Skip(safeSkip)
                .Limit(safeTake)
                .ToListAsync();
        }

        public async Task<long> CountPublicAsync(bool includePrivate)
        {
            var filter = includePrivate
                ? Builders<YouTubeContent>.Filter.Empty
                : Builders<YouTubeContent>.Filter.Eq(x => x.IsPrivate, false);
            return await _collection.CountDocumentsAsync(filter);
        }

        public async Task<YouTubeContent?> GetByUrlAsync(string url, bool includePrivate)
        {
            var filter = Builders<YouTubeContent>.Filter.Eq(x => x.Url, url);
            if (!includePrivate)
            {
                filter &= Builders<YouTubeContent>.Filter.Eq(x => x.IsPrivate, false);
            }

            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IList<YouTubeContent>> GetByIdsAsync(IList<string> ids, bool includePrivate)
        {
            if (ids == null || ids.Count == 0)
            {
                return [];
            }

            var filter = Builders<YouTubeContent>.Filter.In(x => x.Id, ids);
            if (!includePrivate)
            {
                filter &= Builders<YouTubeContent>.Filter.Eq(x => x.IsPrivate, false);
            }

            return await _collection.Find(filter).ToListAsync();
        }
    }
    public class QueryLinkRepository : BaseRepository<QueryLink>, IQueryLinkRepository
    {
        public QueryLinkRepository(IMongoDatabase database) : base(database, DbCollections.QueryLinks)
        {
        }
    }
    public class PublishScheduleRepository : BaseRepository<PublishSchedule>, IPublishScheduleRepository
    {
        public PublishScheduleRepository(IMongoDatabase database) : base(database, DbCollections.PublishSchedules)
        {
        }
    }
    public class PageRepository : BaseRepository<Page>, IPageRepository
    {
        public PageRepository(IMongoDatabase database) : base(database, DbCollections.Pages)
        {
        }

        public async Task<Page?> GetByUrlAsync(string url)
            => await _collection.Find(x => x.Url == url).FirstOrDefaultAsync();
    }
    public class SponsorRepository : BaseRepository<Sponsor>, ISponsorRepository
    {
        public SponsorRepository(IMongoDatabase database) : base(database, DbCollections.Sponsors)
        {
        }
    }
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(IMongoDatabase database) : base(database, DbCollections.Products)
        {
        }

        public async Task<IList<Product>> GetPublicOrderedAsync(int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            return await _collection
                .Find(_ => true)
                .SortByDescending(x => x.CreationDateTime)
                .Skip(safeSkip)
                .Limit(safeTake)
                .ToListAsync();
        }
    }
    public class ProductCategoryRepository : BaseRepository<ProductCategory>, IProductCategoryRepository
    {
        public ProductCategoryRepository(IMongoDatabase database) : base(database, DbCollections.ProductCategories)
        {
        }
    }

    public class CalendarEventRepository : BaseRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(IMongoDatabase database) : base(database, DbCollections.CalendarEvents)
        {
        }

        public async Task<IList<CalendarEvent>> GetRecentAsync(DateTime fromInclusive, int limit)
        {
            var safeLimit = Math.Clamp(limit, 1, 250);
            return await _collection
                .Find(x => x.CreationDateTime >= fromInclusive)
                .SortByDescending(x => x.CreationDateTime)
                .Limit(safeLimit)
                .ToListAsync();
        }
    }

    public class CompilationRepository : BaseRepository<Compilation>, ICompilationRepository
    {
        public CompilationRepository(IMongoDatabase database) : base(database, DbCollections.Compilations)
        {
        }

        public async Task<Compilation?> GetByUrlAsync(string url)
            => await _collection.Find(x => x.Url == url).FirstOrDefaultAsync();
    }

    public class ShortLinkRepository : BaseRepository<ShortLink>, IShortLinkRepository
    {
        public ShortLinkRepository(IMongoDatabase database) : base(database, DbCollections.ShortLinks)
        {
        }

        public async Task<ShortLink?> GetByCodeAsync(string code)
        {
            var normalizedCode = ShortLink.NormalizeCode(code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return null;
            }

            var filter = Builders<ShortLink>.Filter.Regex(x => x.Code, new BsonRegularExpression($"^{Regex.Escape(normalizedCode)}$", "i"));
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<int> IncrementClicksAsync(string id)
        {
            var filter = ObjectId.TryParse(id, out var objectId)
                ? Builders<ShortLink>.Filter.Eq("_id", objectId)
                : Builders<ShortLink>.Filter.Eq("_id", id);
            var update = Builders<ShortLink>.Update.Inc(x => x.ClicksCount, 1);
            var options = new FindOneAndUpdateOptions<ShortLink> { ReturnDocument = ReturnDocument.After };
            var updated = await _collection.FindOneAndUpdateAsync(filter, update, options);
            return updated?.ClicksCount ?? 0;
        }
    }

    public class ConfigurationRepository : BaseRepository<MorWalPizConfiguration>, IConfigurationRepository
    {
        public ConfigurationRepository(IMongoDatabase database) : base(database, DbCollections.Configurations)
        {
        }
    }
    public class YTChannelRepository : BaseRepository<YTChannel>, IYTChannelRepository
    {
        public YTChannelRepository(IMongoDatabase database) : base(database, DbCollections.Channels)
        {
        }
    }

    public class SponsorApplyRepository : BaseRepository<SponsorApply>, ISponsorApplyRepository
    {
        public SponsorApplyRepository(IMongoDatabase database) : base(database, DbCollections.SponsorApplies)
        {
        }
    }
    public class BioLinkRepository : BaseRepository<BioLink>, IBioLinkRepository
    {
        public BioLinkRepository(IMongoDatabase database) : base(database, DbCollections.BioLinks)
        {
        }
    }
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(IMongoDatabase database) : base(database, DbCollections.Categories)
        {
        }
    }

    public class CustomFormRepository : BaseRepository<CustomForm>, ICustomFormRepository
    {
        public CustomFormRepository(IMongoDatabase database) : base(database, DbCollections.CustomForms)
        {
        }

        public async Task<IList<CustomForm>> GetActiveAsync()
            => await _collection.Find(x => x.Active).ToListAsync();

        public async Task<CustomForm?> GetByUrlAsync(string url)
        {
            var escaped = Regex.Escape(url.Trim());
            var filter = Builders<CustomForm>.Filter.Regex(x => x.Url, new BsonRegularExpression($"^{escaped}$", "i"));
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IList<CustomForm>> GetBatchAsync(string? continuationToken, int batchSize)
        {
            var safeBatchSize = Math.Clamp(batchSize, 1, 200);
            var filter = string.IsNullOrWhiteSpace(continuationToken)
                ? Builders<CustomForm>.Filter.Empty
                : Builders<CustomForm>.Filter.Gt(x => x.Id, continuationToken);

            return await _collection
                .Find(filter)
                .SortBy(x => x.Id)
                .Limit(safeBatchSize)
                .ToListAsync();
        }
    }

    public class CustomFormResponseRepository : BaseRepository<CustomFormResponseDocument>, ICustomFormResponseRepository
    {
        public CustomFormResponseRepository(IMongoDatabase database) : base(database, DbCollections.CustomFormResponses)
        {
        }

        public async Task<IList<CustomFormResponseDocument>> GetByFormIdAsync(string formId, int limit = 500)
        {
            var safeLimit = Math.Clamp(limit, 1, 5000);
            return await _collection
                .Find(x => x.FormId == formId)
                .SortByDescending(x => x.SubmittedAt)
                .Limit(safeLimit)
                .ToListAsync();
        }

        public async Task<int> CountByFormIdAsync(string formId)
            => (int)await _collection.CountDocumentsAsync(x => x.FormId == formId);

        public async Task<bool> ExistsForFormAsync(string formId)
            => await _collection.Find(x => x.FormId == formId).Limit(1).AnyAsync();

        public async Task<bool> UpsertByFormAndResponseIdAsync(CustomFormResponseDocument item)
        {
            var filter = Builders<CustomFormResponseDocument>.Filter.Eq(x => x.FormId, item.FormId)
                & Builders<CustomFormResponseDocument>.Filter.Eq(x => x.ResponseId, item.ResponseId);
            var result = await _collection.ReplaceOneAsync(filter, item, new ReplaceOptions { IsUpsert = true });
            return result.UpsertedId != null;
        }
    }

    public class DigitalProductRepository : BaseRepository<DigitalProduct>, IDigitalProductRepository
    {
        public DigitalProductRepository(IMongoDatabase database) : base(database, DbCollections.DigitalProducts)
        {
        }

        public async Task<IList<DigitalProduct>> GetPublicCatalogAsync(int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            return await _collection
                .Find(x => x.IsActive)
                .SortByDescending(x => x.CreationDateTime)
                .Skip(safeSkip)
                .Limit(safeTake)
                .ToListAsync();
        }

        public async Task<IList<DigitalProduct>> GetByCategoryIdAsync(string categoryId, int limit = 500)
        {
            var safeLimit = Math.Clamp(limit, 1, 2000);
            return await _collection
                .Find(x => x.CategoryIds.Contains(categoryId))
                .Limit(safeLimit)
                .ToListAsync();
        }
    }

    public class DigitalProductCategoryRepository : BaseRepository<DigitalProductCategory>, IDigitalProductCategoryRepository
    {
        public DigitalProductCategoryRepository(IMongoDatabase database) : base(database, DbCollections.DigitalProductCategories)
        {
        }

        public async Task<IList<DigitalProductCategory>> GetOrderedAsync(int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            return await _collection
                .Find(_ => true)
                .SortBy(x => x.DisplayOrder)
                .Skip(safeSkip)
                .Limit(safeTake)
                .ToListAsync();
        }
    }

    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(IMongoDatabase database) : base(database, DbCollections.Customers)
        {
        }
    }

    public class CartRepository : BaseRepository<Cart>, ICartRepository
    {
        public CartRepository(IMongoDatabase database) : base(database, DbCollections.Carts)
        {
        }
    }

    public class InsightTopicRepository : BaseRepository<InsightTopic>, IInsightTopicRepository
    {
        public InsightTopicRepository(IMongoDatabase database) : base(database, DbCollections.InsightTopics)
        {
        }
    }

    public class InsightNewsItemRepository : BaseRepository<InsightNewsItem>, IInsightNewsItemRepository
    {
        public InsightNewsItemRepository(IMongoDatabase database) : base(database, DbCollections.InsightNewsItems)
        {
        }
    }

    public class InsightContentPlanRepository : BaseRepository<InsightContentPlan>, IInsightContentPlanRepository
    {
        public InsightContentPlanRepository(IMongoDatabase database) : base(database, DbCollections.InsightContentPlans)
        {
        }
    }

    public class InsightSourceCursorRepository : BaseRepository<InsightSourceCursor>, IInsightSourceCursorRepository
    {
        public InsightSourceCursorRepository(IMongoDatabase database) : base(database, DbCollections.InsightSourceCursors)
        {
        }
    }

    public class InsightCommentAnalysisRunRepository : BaseRepository<InsightCommentAnalysisRun>, IInsightCommentAnalysisRunRepository
    {
        public InsightCommentAnalysisRunRepository(IMongoDatabase database) : base(database, DbCollections.InsightCommentAnalysisRuns)
        {
        }
    }

    public class CompetitionRepository : BaseRepository<Competition>, ICompetitionRepository
    {
        public CompetitionRepository(IMongoDatabase database) : base(database, DbCollections.Competitions)
        {
        }
    }

    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(IMongoDatabase database) : base(database, DbCollections.Users)
        {
        }

        public async Task<User?> FindByUsernameAsync(string username)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Email, email);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await FindByUsernameAsync(username) ?? await FindByEmailAsync(username);

            if (user == null)
                return null;

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash, user.Salt))
                return null;

            return user;
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            return PasswordHashing.VerifyPassword(password, hash, salt);
        }

        public static string HashPassword(string password, out string salt)
        {
            return PasswordHashing.HashPassword(password, out salt);
        }
    }

    public class UserGroupRepository : BaseRepository<UserGroup>, IUserGroupRepository
    {
        public UserGroupRepository(IMongoDatabase database) : base(database, DbCollections.UserGroups)
        {
        }

        public async Task<UserGroup?> GetByCodeAsync(string code)
        {
            var normalizedCode = code.Trim().ToLowerInvariant();
            return await _collection.Find(group => group.Code.ToLower() == normalizedCode).FirstOrDefaultAsync();
        }

        public async Task<IList<UserGroup>> GetByIdsAsync(IList<string> groupIds)
        {
            if (groupIds.Count == 0)
            {
                return [];
            }

            return await _collection.Find(group => groupIds.Contains(group.Id)).ToListAsync();
        }
    }

    public class ImpersonationGrantRepository : BaseRepository<ImpersonationGrant>, IImpersonationGrantRepository
    {
        public ImpersonationGrantRepository(IMongoDatabase database) : base(database, DbCollections.ImpersonationGrants)
        {
        }

        public Task<ImpersonationGrant?> GetByHashAsync(string grantHash)
            => _collection.Find(x => x.GrantHash == grantHash).FirstOrDefaultAsync();

        public Task<ImpersonationGrant?> RedeemAsync(string grantHash, string sessionId, DateTime redeemedAt)
        {
            var filter = Builders<ImpersonationGrant>.Filter.And(
                Builders<ImpersonationGrant>.Filter.Eq(x => x.GrantHash, grantHash),
                Builders<ImpersonationGrant>.Filter.Eq(x => x.RedeemedAt, null),
                Builders<ImpersonationGrant>.Filter.Gt(x => x.ExpiresAt, redeemedAt));
            var update = Builders<ImpersonationGrant>.Update
                .Set(x => x.RedeemedAt, redeemedAt)
                .Set(x => x.SessionId, sessionId);
            return _collection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<ImpersonationGrant> { ReturnDocument = ReturnDocument.After });
        }
    }

    public class ImpersonationSessionRepository : BaseRepository<ImpersonationSession>, IImpersonationSessionRepository
    {
        public ImpersonationSessionRepository(IMongoDatabase database) : base(database, DbCollections.ImpersonationSessions)
        {
        }

        public Task<ImpersonationSession?> GetByHashAsync(string sessionHash)
            => _collection.Find(x => x.SessionHash == sessionHash).FirstOrDefaultAsync();

        public async Task<bool> EndAsync(string sessionHash, DateTime endedAt, string reason)
        {
            var filter = Builders<ImpersonationSession>.Filter.And(
                Builders<ImpersonationSession>.Filter.Eq(x => x.SessionHash, sessionHash),
                Builders<ImpersonationSession>.Filter.Eq(x => x.EndedAt, null));
            var update = Builders<ImpersonationSession>.Update
                .Set(x => x.EndedAt, endedAt)
                .Set(x => x.EndReason, reason);
            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount == 1;
        }
    }

    public class ImpersonationAuditRepository : BaseRepository<ImpersonationAuditEvent>, IImpersonationAuditRepository
    {
        public ImpersonationAuditRepository(IMongoDatabase database) : base(database, DbCollections.ImpersonationAuditEvents)
        {
        }
    }

    public class LoginAttemptRepository : BaseRepository<LoginAttempt>, ILoginAttemptRepository
    {
        public LoginAttemptRepository(IMongoDatabase database) : base(database, DbCollections.LoginAttempts)
        {
        }

        public async Task<List<LoginAttempt>> GetRecentAttemptsByIpAsync(string ipAddress, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.IpAddress, ipAddress),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );

            return await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        public async Task<List<LoginAttempt>> GetRecentAttemptsByUsernameAsync(string username, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.Username, username),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );

            return await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        public async Task<int> GetFailedAttemptsCountByIpAsync(string ipAddress, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.IpAddress, ipAddress),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );

            return (int)await _collection.CountDocumentsAsync(filter);
        }

        public async Task<int> GetFailedAttemptsCountByUsernameAsync(string username, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.Username, username),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );

            return (int)await _collection.CountDocumentsAsync(filter);
        }

        public async Task<DateTime?> GetLastFailedAttemptTimeByIpAsync(string ipAddress)
        {
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.IpAddress, ipAddress),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false)
            );

            var lastAttempt = await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .FirstOrDefaultAsync();

            return lastAttempt?.AttemptTime;
        }

        public async Task<DateTime?> GetLastFailedAttemptTimeByUsernameAsync(string username)
        {
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.Username, username),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false)
            );

            var lastAttempt = await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .FirstOrDefaultAsync();

            return lastAttempt?.AttemptTime;
        }

        public async Task CleanupOldAttemptsAsync(TimeSpan olderThan)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(olderThan);
            var filter = Builders<LoginAttempt>.Filter.Lt(a => a.AttemptTime, cutoffTime);

            await _collection.DeleteManyAsync(filter);
        }
    }

    public class UserChannelRepository : BaseRepository<UserChannel>, IUserChannelRepository
    {
        public UserChannelRepository(IMongoDatabase database) : base(database, DbCollections.UserChannels) { }

        public async Task<IList<UserChannel>> GetByUserIdAsync(string userId)
            => await GetItemsAsync(uc => uc.UserId == userId && uc.IsActive);

        public async Task<IList<UserChannel>> GetByChannelIdAsync(string channelId)
            => await GetItemsAsync(uc => uc.ChannelId == channelId && uc.IsActive);

        public async Task<UserChannel?> GetByUserAndChannelAsync(string userId, string channelId)
        {
            var results = await GetItemsAsync(uc => uc.UserId == userId && uc.ChannelId == channelId);
            return results.FirstOrDefault();
        }
    }

    public class UserChannelOwnerRepository : BaseRepository<UserChannelOwner>, IUserChannelOwnerRepository
    {
        public UserChannelOwnerRepository(IMongoDatabase database) : base(database, DbCollections.UserChannelOwners) { }

        public async Task<IList<UserChannelOwner>> GetByUserIdAsync(string userId)
            => await GetItemsAsync(o => o.UserId == userId && o.IsActive);

        public async Task<IList<UserChannelOwner>> GetByChannelIdAsync(string channelId)
            => await GetItemsAsync(o => o.ChannelId == channelId && o.IsActive);
    }

    public class UserRequestRepository : BaseRepository<UserRequest>, IUserRequestRepository
    {
        public UserRequestRepository(IMongoDatabase database) : base(database, DbCollections.UserRequests) { }
    }
}