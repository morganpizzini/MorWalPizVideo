using MongoDB.Bson;
using MorWalPizVideo.Domain.BlogInteractions;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class BlogInteractionServiceTests
{
    private const string AccountId = "800000000000000000000001";
    private const string PostId = "900000000000000000000001";

    [Fact]
    public void Comment_list_sort_uses_one_field_for_Cosmos_compatibility()
    {
        Assert.Equal(new BsonDocument { { "creationDateTime", -1 } },
            BlogInteractionRepository.CommentListSortKeys);
    }

    [Fact]
    public async Task Comment_submission_is_idempotent_and_requires_moderation()
    {
        var (service, repository) = await CreateServiceAsync();

        var first = await service.AddCommentAsync(PrimaryScenario.ChannelId, PostId, "article", AccountId,
            "Reader", "  useful article  ", "request-123456");
        var retry = await service.AddCommentAsync(PrimaryScenario.ChannelId, PostId, "article", AccountId,
            "Reader", "useful article", "request-123456");

        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(BlogCommentStatus.Pending, first.Status);
        Assert.Equal(1, await repository.CountCommentsAsync(PrimaryScenario.ChannelId, PostId, null));

        var approved = await service.ModerateCommentAsync(PrimaryScenario.ChannelId, first.Id, first.Revision,
            BlogCommentStatus.Approved, "moderator");
        Assert.Equal(BlogCommentStatus.Approved, approved!.Status);
        Assert.Equal(1, await repository.CountCommentsAsync(PrimaryScenario.ChannelId, PostId, BlogCommentStatus.Approved));
    }

    [Fact]
    public async Task Channel_comment_block_does_not_hide_approved_comments_or_block_reactions()
    {
        var (service, repository) = await CreateServiceAsync();
        var comment = await service.AddCommentAsync(PrimaryScenario.ChannelId, PostId, "article", AccountId,
            "Reader", "approved comment", "request-123456");
        await service.ModerateCommentAsync(PrimaryScenario.ChannelId, comment.Id, comment.Revision,
            BlogCommentStatus.Approved, "moderator");

        await service.UpdateAccessAsync(PrimaryScenario.ChannelId, AccountId, commentsBlocked: true,
            portalAccessRevoked: false, revision: 0, reason: "spam", actorId: "moderator");

        await Assert.ThrowsAsync<BlogInteractionForbiddenException>(() =>
            service.AddCommentAsync(PrimaryScenario.ChannelId, PostId, "article", AccountId, "Reader",
                "another comment", "request-654321"));
        await service.SetReactionAsync(PrimaryScenario.ChannelId, PostId, "article", AccountId, BlogReactionValue.Like);

        Assert.Equal(1, await repository.CountCommentsAsync(PrimaryScenario.ChannelId, PostId, BlogCommentStatus.Approved));
        Assert.Equal((1L, 0L), await repository.CountReactionsAsync(PrimaryScenario.ChannelId, PostId));
    }

    [Fact]
    public async Task Portal_revocation_blocks_reactions_until_access_is_restored()
    {
        var (service, _) = await CreateServiceAsync();
        await service.UpdateAccessAsync(PrimaryScenario.ChannelId, AccountId, commentsBlocked: false,
            portalAccessRevoked: true, revision: 0, reason: "abuse", actorId: "moderator");

        await Assert.ThrowsAsync<BlogInteractionForbiddenException>(() =>
            service.SetReactionAsync(PrimaryScenario.ChannelId, PostId, "article", AccountId, BlogReactionValue.Dislike));
    }

    private static async Task<(BlogInteractionService Service, IBlogInteractionRepository Repository)> CreateServiceAsync()
    {
        var scenario = new PrimaryScenario();
        IPublicAccountRepository accounts = new PublicAccountMockRepository(scenario);
        await accounts.AddAccountAsync(new PublicAccount
        {
            Id = AccountId,
            Username = "reader",
            NormalizedUsername = "READER",
            Email = "reader@example.test",
            NormalizedEmail = "READER@EXAMPLE.TEST",
            DisplayName = "Reader",
            IsActive = true
        });
        IBlogInteractionRepository interactions = new BlogInteractionMockRepository(scenario);
        return (new BlogInteractionService(interactions, accounts, TimeProvider.System), interactions);
    }
}
