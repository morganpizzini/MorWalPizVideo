using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.BlogInteractions;

public interface IBlogInteractionRepository
{
    Task<IReadOnlyList<BlogComment>> GetCommentsAsync(string channelId, string? postId, string? accountId,
        BlogCommentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<long> CountCommentsAsync(string channelId, string? postId, BlogCommentStatus? status, string? accountId = null,
        CancellationToken cancellationToken = default);
    Task<BlogComment?> GetCommentAsync(string channelId, string id, CancellationToken cancellationToken = default);
    Task<BlogComment?> FindCommentRequestAsync(string channelId, string accountId, string requestId,
        CancellationToken cancellationToken = default);
    Task AddCommentAsync(BlogComment comment, CancellationToken cancellationToken = default);
    Task<bool> ReplaceCommentAsync(BlogComment comment, long expectedRevision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogReaction>> GetReactionsAsync(string channelId, string? postId, string? accountId,
        CancellationToken cancellationToken = default);
    Task<(long Likes, long Dislikes)> CountReactionsAsync(string channelId, string postId,
        CancellationToken cancellationToken = default);
    Task SetReactionAsync(BlogReaction reaction, CancellationToken cancellationToken = default);
    Task DeleteReactionAsync(string channelId, string postId, string accountId, CancellationToken cancellationToken = default);
    Task<BlogPortalAccess?> GetAccessAsync(string channelId, string accountId, CancellationToken cancellationToken = default);
    Task<bool> ReplaceAccessAsync(BlogPortalAccess access, long expectedRevision, CancellationToken cancellationToken = default);
}

public sealed class BlogInteractionService(
    IBlogInteractionRepository repository,
    IPublicAccountRepository accounts,
    TimeProvider timeProvider)
{
    public async Task<BlogComment> AddCommentAsync(
        string channelId, string postId, string slug, string accountId, string displayName,
        string body, string requestId, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetAccountAsync(accountId, cancellationToken);
        if (account is not { IsActive: true }) throw new BlogInteractionForbiddenException("An active account is required.");
        var access = await GetAccessAsync(channelId, accountId, cancellationToken);
        if (access.PortalAccessRevoked || access.CommentsBlocked)
            throw new BlogInteractionForbiddenException("Comment access is unavailable.");
        var normalizedBody = body.Trim();
        if (normalizedBody.Length is < 1 or > 2000)
            throw new BlogInteractionValidationException("Comments must contain 1 to 2000 characters.");
        if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 80)
            throw new BlogInteractionValidationException("A valid submission request ID is required.");

        var existing = await repository.FindCommentRequestAsync(channelId, accountId, requestId, cancellationToken);
        if (existing is not null)
        {
            if (existing.PostId == postId && existing.Body == normalizedBody) return existing;
            throw new BlogInteractionConflictException("The request ID was already used for different content.");
        }

        var comment = new BlogComment
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ChannelId = channelId,
            PostId = postId,
            Slug = slug,
            AccountId = accountId,
            DisplayName = displayName,
            Body = normalizedBody,
            RequestId = requestId,
            CreationDateTime = timeProvider.GetUtcNow().UtcDateTime
        };
        try { await repository.AddCommentAsync(comment, cancellationToken); }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            existing = await repository.FindCommentRequestAsync(channelId, accountId, requestId, cancellationToken);
            if (existing is not null && existing.PostId == postId && existing.Body == normalizedBody) return existing;
            throw new BlogInteractionConflictException("The comment request conflicts with an existing submission.");
        }
        catch (BlogInteractionDuplicateRequestException)
        {
            existing = await repository.FindCommentRequestAsync(channelId, accountId, requestId, cancellationToken);
            if (existing is not null && existing.PostId == postId && existing.Body == normalizedBody) return existing;
            throw new BlogInteractionConflictException("The comment request conflicts with an existing submission.");
        }
        return comment;
    }

    public async Task<BlogComment?> WithdrawCommentAsync(string channelId, string id, string accountId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(channelId, accountId, cancellationToken);
        if (access.PortalAccessRevoked || access.CommentsBlocked)
            throw new BlogInteractionForbiddenException("Comment access is unavailable.");
        var comment = await repository.GetCommentAsync(channelId, id, cancellationToken);
        if (comment is null || comment.AccountId != accountId) return null;
        if (comment.Status == BlogCommentStatus.Withdrawn) return comment;
        var withdrawn = comment with { Status = BlogCommentStatus.Withdrawn, Body = string.Empty, Revision = comment.Revision + 1 };
        if (!await repository.ReplaceCommentAsync(withdrawn, comment.Revision, cancellationToken))
            throw new BlogInteractionConflictException("Comment changed. Reload before withdrawing.");
        return withdrawn;
    }

    public async Task<BlogReaction?> SetReactionAsync(string channelId, string postId, string slug, string accountId,
        BlogReactionValue? value, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetAccountAsync(accountId, cancellationToken);
        if (account is not { IsActive: true }) throw new BlogInteractionForbiddenException("An active account is required.");
        var access = await GetAccessAsync(channelId, accountId, cancellationToken);
        if (access.PortalAccessRevoked) throw new BlogInteractionForbiddenException("Portal access is unavailable.");
        if (value is null)
        {
            await repository.DeleteReactionAsync(channelId, postId, accountId, cancellationToken);
            return null;
        }
        var current = (await repository.GetReactionsAsync(channelId, postId, accountId, cancellationToken)).FirstOrDefault();
        var reaction = current is null
            ? new BlogReaction { Id = ObjectId.GenerateNewId().ToString(), ChannelId = channelId, PostId = postId, Slug = slug, AccountId = accountId, Value = value.Value, CreationDateTime = timeProvider.GetUtcNow().UtcDateTime }
            : current with { Value = value.Value, Revision = current.Revision + 1 };
        await repository.SetReactionAsync(reaction, cancellationToken);
        return reaction;
    }

    public async Task<BlogPortalAccess> GetAccessAsync(string channelId, string accountId,
        CancellationToken cancellationToken = default) =>
        await repository.GetAccessAsync(channelId, accountId, cancellationToken) ??
        new BlogPortalAccess { ChannelId = channelId, AccountId = accountId, Revision = 0 };

    public async Task<BlogPortalAccess> UpdateAccessAsync(string channelId, string accountId,
        bool commentsBlocked, bool portalAccessRevoked, long revision, string reason, string actorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new BlogInteractionValidationException("An internal reason of 1 to 500 characters is required.");
        var current = await GetAccessAsync(channelId, accountId, cancellationToken);
        if (current.Revision != revision) throw new BlogInteractionConflictException("Portal access changed. Reload before updating.");
        var updated = current with
        {
            Id = string.IsNullOrWhiteSpace(current.Id) ? ObjectId.GenerateNewId().ToString() : current.Id,
            CommentsBlocked = commentsBlocked,
            PortalAccessRevoked = portalAccessRevoked,
            Revision = revision + 1,
            Reason = reason.Trim(),
            UpdatedBy = actorId,
            UpdatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        if (!await repository.ReplaceAccessAsync(updated, revision, cancellationToken))
            throw new BlogInteractionConflictException("Portal access changed. Reload before updating.");
        return updated;
    }

    public async Task<BlogComment?> ModerateCommentAsync(string channelId, string id, long revision,
        BlogCommentStatus status, string moderatorId, CancellationToken cancellationToken = default)
    {
        if (status is not (BlogCommentStatus.Approved or BlogCommentStatus.Rejected or BlogCommentStatus.Hidden))
            throw new BlogInteractionValidationException("Invalid moderation status.");
        var current = await repository.GetCommentAsync(channelId, id, cancellationToken);
        if (current is null) return null;
        if (current.Status == BlogCommentStatus.Withdrawn)
            throw new BlogInteractionConflictException("Withdrawn comments cannot be restored.");
        var updated = current with
        {
            Status = status,
            ModeratedBy = moderatorId,
            ModeratedAt = timeProvider.GetUtcNow().UtcDateTime,
            Revision = revision + 1
        };
        if (!await repository.ReplaceCommentAsync(updated, revision, cancellationToken))
            throw new BlogInteractionConflictException("Comment changed. Reload before moderating.");
        return updated;
    }
}

public sealed class BlogInteractionValidationException(string message) : Exception(message);
public sealed class BlogInteractionConflictException(string message) : Exception(message);
public sealed class BlogInteractionForbiddenException(string message) : Exception(message);
public sealed class BlogInteractionDuplicateRequestException : Exception;

public sealed class BlogInteractionRepository(IMongoDatabase database) : IBlogInteractionRepository
{
    private readonly IMongoCollection<BlogComment> comments = database.GetCollection<BlogComment>(DbCollections.BlogComments);
    private readonly IMongoCollection<BlogReaction> reactions = database.GetCollection<BlogReaction>(DbCollections.BlogReactions);
    private readonly IMongoCollection<BlogPortalAccess> access = database.GetCollection<BlogPortalAccess>(DbCollections.BlogPortalAccess);

    public async Task<IReadOnlyList<BlogComment>> GetCommentsAsync(string channelId, string? postId, string? accountId, BlogCommentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BlogComment>.Filter.Eq(x => x.ChannelId, channelId);
        if (postId is not null) filter &= Builders<BlogComment>.Filter.Eq(x => x.PostId, postId);
        if (accountId is not null) filter &= Builders<BlogComment>.Filter.Eq(x => x.AccountId, accountId);
        if (status.HasValue) filter &= Builders<BlogComment>.Filter.Eq(x => x.Status, status.Value);
        return await comments.Find(filter).SortByDescending(x => x.CreationDateTime).ThenByDescending(x => x.Id)
            .Skip((Math.Clamp(page, 1, 10000) - 1) * Math.Clamp(pageSize, 1, 50))
            .Limit(Math.Clamp(pageSize, 1, 50)).ToListAsync(cancellationToken);
    }
    public Task<long> CountCommentsAsync(string channelId, string? postId, BlogCommentStatus? status, string? accountId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BlogComment>.Filter.Eq(x => x.ChannelId, channelId);
        if (postId is not null) filter &= Builders<BlogComment>.Filter.Eq(x => x.PostId, postId);
        if (accountId is not null) filter &= Builders<BlogComment>.Filter.Eq(x => x.AccountId, accountId);
        if (status.HasValue) filter &= Builders<BlogComment>.Filter.Eq(x => x.Status, status.Value);
        return comments.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
    public Task<BlogComment?> GetCommentAsync(string channelId, string id, CancellationToken cancellationToken = default) =>
        comments.Find(x => x.ChannelId == channelId && x.Id == id).FirstOrDefaultAsync(cancellationToken);
    public Task<BlogComment?> FindCommentRequestAsync(string channelId, string accountId, string requestId, CancellationToken cancellationToken = default) =>
        comments.Find(x => x.ChannelId == channelId && x.AccountId == accountId && x.RequestId == requestId).FirstOrDefaultAsync(cancellationToken);
    public Task AddCommentAsync(BlogComment comment, CancellationToken cancellationToken = default) => comments.InsertOneAsync(comment, cancellationToken: cancellationToken);
    public async Task<bool> ReplaceCommentAsync(BlogComment comment, long expectedRevision, CancellationToken cancellationToken = default) =>
        (await comments.ReplaceOneAsync(x => x.ChannelId == comment.ChannelId && x.Id == comment.Id && x.Revision == expectedRevision, comment, cancellationToken: cancellationToken)).ModifiedCount == 1;
    public async Task<IReadOnlyList<BlogReaction>> GetReactionsAsync(string channelId, string? postId, string? accountId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BlogReaction>.Filter.Eq(x => x.ChannelId, channelId);
        if (postId is not null) filter &= Builders<BlogReaction>.Filter.Eq(x => x.PostId, postId);
        if (accountId is not null) filter &= Builders<BlogReaction>.Filter.Eq(x => x.AccountId, accountId);
        return await reactions.Find(filter).ToListAsync(cancellationToken);
    }
    public async Task<(long Likes, long Dislikes)> CountReactionsAsync(string channelId, string postId, CancellationToken cancellationToken = default)
    {
        var baseFilter = Builders<BlogReaction>.Filter.Eq(x => x.ChannelId, channelId) &
                         Builders<BlogReaction>.Filter.Eq(x => x.PostId, postId);
        var likes = await reactions.CountDocumentsAsync(baseFilter & Builders<BlogReaction>.Filter.Eq(x => x.Value, BlogReactionValue.Like), cancellationToken: cancellationToken);
        var dislikes = await reactions.CountDocumentsAsync(baseFilter & Builders<BlogReaction>.Filter.Eq(x => x.Value, BlogReactionValue.Dislike), cancellationToken: cancellationToken);
        return (likes, dislikes);
    }
    public async Task SetReactionAsync(BlogReaction reaction, CancellationToken cancellationToken = default)
    {
        try
        {
            await reactions.ReplaceOneAsync(x => x.ChannelId == reaction.ChannelId && x.PostId == reaction.PostId && x.AccountId == reaction.AccountId,
                reaction, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            await reactions.ReplaceOneAsync(x => x.ChannelId == reaction.ChannelId && x.PostId == reaction.PostId && x.AccountId == reaction.AccountId,
                reaction, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
    }
    public Task DeleteReactionAsync(string channelId, string postId, string accountId, CancellationToken cancellationToken = default) =>
        reactions.DeleteOneAsync(x => x.ChannelId == channelId && x.PostId == postId && x.AccountId == accountId, cancellationToken);
    public Task<BlogPortalAccess?> GetAccessAsync(string channelId, string accountId, CancellationToken cancellationToken = default) =>
        access.Find(x => x.ChannelId == channelId && x.AccountId == accountId).FirstOrDefaultAsync(cancellationToken);
    public async Task<bool> ReplaceAccessAsync(BlogPortalAccess item, long expectedRevision, CancellationToken cancellationToken = default)
    {
        if (expectedRevision == 0)
        {
            try { await access.InsertOneAsync(item, cancellationToken: cancellationToken); return true; }
            catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000) { return false; }
        }
        return (await access.ReplaceOneAsync(x => x.ChannelId == item.ChannelId && x.AccountId == item.AccountId && x.Revision == expectedRevision, item, cancellationToken: cancellationToken)).ModifiedCount == 1;
    }
}

public sealed class BlogInteractionMockRepository(IMockScenario scenario) : IBlogInteractionRepository
{
    private readonly object gate = new();
    public Task<IReadOnlyList<BlogComment>> GetCommentsAsync(string channelId, string? postId, string? accountId, BlogCommentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        lock (gate) return Task.FromResult<IReadOnlyList<BlogComment>>(scenario.Read<BlogComment>(DbCollections.BlogComments)
            .Where(x => x.ChannelId == channelId && (postId is null || x.PostId == postId) && (accountId is null || x.AccountId == accountId) && (!status.HasValue || x.Status == status))
            .OrderByDescending(x => x.CreationDateTime).ThenByDescending(x => x.Id).Skip((Math.Clamp(page, 1, 10000) - 1) * Math.Clamp(pageSize, 1, 50)).Take(Math.Clamp(pageSize, 1, 50)).ToArray());
    }
    public Task<long> CountCommentsAsync(string channelId, string? postId, BlogCommentStatus? status, string? accountId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult((long)scenario.Read<BlogComment>(DbCollections.BlogComments).Count(x => x.ChannelId == channelId && (postId is null || x.PostId == postId) && (accountId is null || x.AccountId == accountId) && (!status.HasValue || x.Status == status)));
    public Task<BlogComment?> GetCommentAsync(string channelId, string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario.Read<BlogComment>(DbCollections.BlogComments).FirstOrDefault(x => x.ChannelId == channelId && x.Id == id));
    public Task<BlogComment?> FindCommentRequestAsync(string channelId, string accountId, string requestId, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario.Read<BlogComment>(DbCollections.BlogComments).FirstOrDefault(x => x.ChannelId == channelId && x.AccountId == accountId && x.RequestId == requestId));
    public Task AddCommentAsync(BlogComment comment, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (scenario.Read<BlogComment>(DbCollections.BlogComments).Any(x => x.ChannelId == comment.ChannelId && x.AccountId == comment.AccountId && x.RequestId == comment.RequestId))
                throw new BlogInteractionDuplicateRequestException();
            scenario.Add(DbCollections.BlogComments, comment);
        }
        return Task.CompletedTask;
    }
    public Task<bool> ReplaceCommentAsync(BlogComment comment, long expectedRevision, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var old = scenario.Read<BlogComment>(DbCollections.BlogComments).FirstOrDefault(x => x.ChannelId == comment.ChannelId && x.Id == comment.Id && x.Revision == expectedRevision);
            if (old is null) return Task.FromResult(false);
            scenario.Replace(DbCollections.BlogComments, comment);
            return Task.FromResult(true);
        }
    }
    public Task<IReadOnlyList<BlogReaction>> GetReactionsAsync(string channelId, string? postId, string? accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BlogReaction>>(scenario.Read<BlogReaction>(DbCollections.BlogReactions).Where(x => x.ChannelId == channelId && (postId is null || x.PostId == postId) && (accountId is null || x.AccountId == accountId)).ToArray());
    public Task<(long Likes, long Dislikes)> CountReactionsAsync(string channelId, string postId, CancellationToken cancellationToken = default)
    {
        var items = scenario.Read<BlogReaction>(DbCollections.BlogReactions).Where(x => x.ChannelId == channelId && x.PostId == postId).ToArray();
        return Task.FromResult(((long)items.Count(x => x.Value == BlogReactionValue.Like), (long)items.Count(x => x.Value == BlogReactionValue.Dislike)));
    }
    public Task SetReactionAsync(BlogReaction reaction, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var old = scenario.Read<BlogReaction>(DbCollections.BlogReactions).FirstOrDefault(x => x.ChannelId == reaction.ChannelId && x.PostId == reaction.PostId && x.AccountId == reaction.AccountId);
            if (old is null) scenario.Add(DbCollections.BlogReactions, reaction);
            else scenario.Replace(DbCollections.BlogReactions, reaction with { Id = old.Id });
        }
        return Task.CompletedTask;
    }
    public Task DeleteReactionAsync(string channelId, string postId, string accountId, CancellationToken cancellationToken = default)
    {
        var old = scenario.Read<BlogReaction>(DbCollections.BlogReactions).FirstOrDefault(x => x.ChannelId == channelId && x.PostId == postId && x.AccountId == accountId);
        if (old is not null) scenario.Delete<BlogReaction>(DbCollections.BlogReactions, old.Id);
        return Task.CompletedTask;
    }
    public Task<BlogPortalAccess?> GetAccessAsync(string channelId, string accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario.Read<BlogPortalAccess>(DbCollections.BlogPortalAccess).FirstOrDefault(x => x.ChannelId == channelId && x.AccountId == accountId));
    public Task<bool> ReplaceAccessAsync(BlogPortalAccess item, long expectedRevision, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var old = scenario.Read<BlogPortalAccess>(DbCollections.BlogPortalAccess).FirstOrDefault(x => x.ChannelId == item.ChannelId && x.AccountId == item.AccountId);
            if (expectedRevision == 0 && old is null) { scenario.Add(DbCollections.BlogPortalAccess, item); return Task.FromResult(true); }
            if (old is null || old.Revision != expectedRevision) return Task.FromResult(false);
            scenario.Replace(DbCollections.BlogPortalAccess, item with { Id = old.Id });
            return Task.FromResult(true);
        }
    }
}
