using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Domain.BlogInteractions;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/blog-interactions")]
[RequireChannelScope]
public sealed class BlogInteractionsController(
    BlogInteractionService interactions,
    IBlogInteractionRepository repository,
    IPublicAccountRepository accounts,
    BlogService blogService) : ControllerBase
{
    private string ChannelId => HttpContext.GetChannelContext().ChannelId;
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "unknown";

    [HttpGet("articles")]
    [AllowUser(AuthorizationPermissionKeys.BlogInteractionsView, AuthorizationPermissionKeys.BlogCommentsModerate)]
    public async Task<ActionResult<BlogArticleInteractionsPageContract>> ListArticles(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 10000 || pageSize is < 1 or > 50) return BadRequest("Invalid pagination.");
        var posts = await blogService.ListAsync(ChannelId, false, page, pageSize + 1);
        var result = await Task.WhenAll(posts.Take(pageSize).Select(async post =>
        {
            var (likes, dislikes) = await repository.CountReactionsAsync(ChannelId, post.Id, cancellationToken);
            var comments = await repository.CountCommentsAsync(ChannelId, post.Id, null, cancellationToken: cancellationToken);
            var snapshot = post.Published ?? post.Draft;
            return new BlogArticleInteractionsContract(post.Id, post.Slug, snapshot.Title,
                post.Published is not null, likes, dislikes, comments);
        }));
        return Ok(new BlogArticleInteractionsPageContract(result, page, pageSize, posts.Count > pageSize));
    }

    [HttpGet("comments")]
    [AllowUser(AuthorizationPermissionKeys.BlogInteractionsView, AuthorizationPermissionKeys.BlogCommentsModerate)]
    public async Task<ActionResult<BlogAdminCommentPageContract>> ListComments(
        [FromQuery] BlogCommentStatus? status, [FromQuery] string? postId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 10000 || pageSize is < 1 or > 100) return BadRequest("Invalid pagination.");
        var items = await repository.GetCommentsAsync(ChannelId, postId, null, status, page, pageSize, cancellationToken);
        var total = await repository.CountCommentsAsync(ChannelId, postId, status, cancellationToken: cancellationToken);
        return Ok(new BlogAdminCommentPageContract(items.Select(ToAdminCommentContract).ToArray(), page, pageSize, total));
    }

    [HttpPut("comments/{id}")]
    [AllowUser(AuthorizationPermissionKeys.BlogCommentsModerate)]
    public async Task<IActionResult> ModerateComment(string id, BlogCommentModerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var comment = await interactions.ModerateCommentAsync(ChannelId, id, request.Revision, request.Status, ActorId, cancellationToken);
            return comment is null ? NotFound() : Ok(ToAdminCommentContract(comment));
        }
        catch (BlogInteractionValidationException exception) { return BadRequest(new ProblemDetails { Title = exception.Message }); }
        catch (BlogInteractionConflictException exception) { return Conflict(new ProblemDetails { Title = exception.Message }); }
    }

    [HttpGet("accounts/{accountId}")]
    [AllowUser(AuthorizationPermissionKeys.BlogInteractionsView, AuthorizationPermissionKeys.PortalAccessManage)]
    public async Task<ActionResult<PublicAccountInteractionDetailContract>> GetAccountInteractions(
        string accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 10000 || pageSize is < 1 or > 100) return BadRequest("Invalid pagination.");
        var account = await accounts.GetAccountAsync(accountId, cancellationToken);
        if (account is null) return NotFound();
        var commentItems = await repository.GetCommentsAsync(ChannelId, null, accountId, null, page, pageSize, cancellationToken);
        var commentCount = await repository.CountCommentsAsync(ChannelId, null, null, accountId, cancellationToken);
        var reactions = await repository.GetReactionsAsync(ChannelId, null, accountId, cancellationToken);
        var access = await interactions.GetAccessAsync(ChannelId, accountId, cancellationToken);
        var accountContract = new PublicAccountAdminDetailContract(account.Id, account.Username, account.DisplayName,
            account.IsActive, account.CreationDateTime, ToAccessContract(access), commentCount,
            reactions.LongCount(x => x.Value == BlogReactionValue.Like),
            reactions.LongCount(x => x.Value == BlogReactionValue.Dislike));
        return Ok(new PublicAccountInteractionDetailContract(accountContract,
            commentItems.Select(ToAdminCommentContract).ToArray(),
            reactions.OrderByDescending(x => x.CreationDateTime)
                .Select(x => new BlogReactionAdminContract(x.PostId, x.Slug, x.Value, x.CreationDateTime)).ToArray(),
            page, pageSize));
    }

    [HttpPut("accounts/{accountId}/access")]
    [AllowUser(AuthorizationPermissionKeys.PortalAccessManage)]
    public async Task<IActionResult> UpdateAccess(string accountId, BlogPortalAccessRequest request,
        CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountAsync(accountId, cancellationToken) is null) return NotFound();
        try
        {
            var access = await interactions.UpdateAccessAsync(ChannelId, accountId, request.CommentsBlocked,
                request.PortalAccessRevoked, request.Revision, request.Reason, ActorId, cancellationToken);
            return Ok(ToAccessContract(access));
        }
        catch (BlogInteractionValidationException exception) { return BadRequest(new ProblemDetails { Title = exception.Message }); }
        catch (BlogInteractionConflictException exception) { return Conflict(new ProblemDetails { Title = exception.Message }); }
    }

    private static BlogAdminCommentContract ToAdminCommentContract(BlogComment comment) =>
        new(comment.Id, comment.AccountId, comment.DisplayName, comment.PostId, comment.Slug,
            comment.Body, comment.Status, comment.Revision, comment.CreationDateTime,
            comment.ModeratedBy, comment.ModeratedAt);

    private static BlogPortalAccessContract ToAccessContract(BlogPortalAccess access) =>
        new(access.ChannelId, access.AccountId, access.CommentsBlocked, access.PortalAccessRevoked,
            access.Revision, access.Reason, access.UpdatedBy, access.UpdatedAt == default ? null : access.UpdatedAt);
}
