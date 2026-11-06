using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Domain.BlogInteractions;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.MvcHelpers.Authentication;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[EnableCors("public-account")]
[Route("api/blog/{slug}/interactions")]
public sealed class BlogInteractionsController(
    BlogService blogService,
    IContentService content,
    IConfiguration configuration,
    BlogInteractionService interactions,
    IBlogInteractionRepository repository,
    IPublicAccountService accounts,
    ILogger<BlogInteractionsController> logger) : ControllerBase
{
    [HttpGet("summary")]
    [AllowAnonymous]
    public async Task<ActionResult<BlogInteractionSummaryContract>> Summary(string slug, CancellationToken cancellationToken)
    {
        var (channelId, post) = await ResolvePostAsync(slug, cancellationToken);
        if (post is null) return NotFound();
        var counts = await repository.CountReactionsAsync(channelId!, post.Id, cancellationToken);
        var comments = await repository.CountCommentsAsync(channelId!, post.Id, BlogCommentStatus.Approved, cancellationToken: cancellationToken);
        return Ok(new BlogInteractionSummaryContract(counts.Likes, counts.Dislikes, comments));
    }

    [HttpGet("comments")]
    [AllowAnonymous]
    public async Task<ActionResult<BlogCommentPageContract>> Comments(
        string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 10000 || pageSize is < 1 or > 50) return BadRequest("Invalid pagination.");
        var (channelId, post) = await ResolvePostAsync(slug, cancellationToken);
        if (post is null) return NotFound();
        var comments = await repository.GetCommentsAsync(channelId!, post.Id, null, BlogCommentStatus.Approved,
            page, pageSize, cancellationToken);
        var total = await repository.CountCommentsAsync(channelId!, post.Id, BlogCommentStatus.Approved, cancellationToken: cancellationToken);
        return Ok(new BlogCommentPageContract(comments.Select(comment =>
            new BlogCommentContract(comment.Id, comment.DisplayName, comment.Body, comment.CreationDateTime)).ToArray(), page, pageSize, total));
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
    public async Task<ActionResult<BlogViewerStateContract>> Me(string slug, CancellationToken cancellationToken)
    {
        var (channelId, post) = await ResolvePostAsync(slug, cancellationToken);
        if (post is null) return NotFound();
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId is null) return Unauthorized();
        var access = await interactions.GetAccessAsync(channelId!, accountId, cancellationToken);
        var reaction = (await repository.GetReactionsAsync(channelId!, post.Id, accountId, cancellationToken)).FirstOrDefault();
        return Ok(new BlogViewerStateContract(access.CommentsBlocked, access.PortalAccessRevoked, reaction?.Value));
    }

    [HttpPost("comments")]
    [Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
    [EnableRateLimiting("public-blog-interactions")]
    public async Task<IActionResult> AddComment(string slug, BlogCommentRequest request, CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request)) return StatusCode(StatusCodes.Status403Forbidden);
        var (channelId, post) = await ResolvePostAsync(slug, cancellationToken);
        if (post is null) return NotFound();
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId is null) return Unauthorized();
        var account = await accounts.GetAccountAsync(accountId, cancellationToken);
        if (account is null) return Unauthorized();
        try
        {
            var comment = await interactions.AddCommentAsync(channelId!, post.Id, post.Slug, accountId,
                account.DisplayName, request.Body, request.RequestId, cancellationToken);
            return Ok(new BlogCommentContract(comment.Id, comment.DisplayName, comment.Body, comment.CreationDateTime));
        }
        catch (BlogInteractionForbiddenException) { return Forbid(); }
        catch (BlogInteractionConflictException exception) { return Conflict(new ProblemDetails { Title = exception.Message }); }
        catch (BlogInteractionValidationException exception) { return BadRequest(new ProblemDetails { Title = exception.Message }); }
    }

    [HttpPut("reaction")]
    [Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
    [EnableRateLimiting("public-blog-interactions")]
    public async Task<IActionResult> SetReaction(string slug, BlogReactionRequest request, CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request)) return StatusCode(StatusCodes.Status403Forbidden);
        var (channelId, post) = await ResolvePostAsync(slug, cancellationToken);
        if (post is null) return NotFound();
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId is null) return Unauthorized();
        if (!Enum.TryParse<BlogReactionValue>(request.Value, true, out var value)) return BadRequest("Invalid reaction.");
        try
        {
            await interactions.SetReactionAsync(channelId!, post.Id, post.Slug, accountId, value, cancellationToken);
            return NoContent();
        }
        catch (BlogInteractionForbiddenException) { return Forbid(); }
    }

    [HttpDelete("reaction")]
    [Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
    [EnableRateLimiting("public-blog-interactions")]
    public async Task<IActionResult> ClearReaction(string slug, CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request)) return StatusCode(StatusCodes.Status403Forbidden);
        var (channelId, post) = await ResolvePostAsync(slug, cancellationToken);
        if (post is null) return NotFound();
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId is null) return Unauthorized();
        try
        {
            await interactions.SetReactionAsync(channelId!, post.Id, post.Slug, accountId, null, cancellationToken);
            return NoContent();
        }
        catch (BlogInteractionForbiddenException) { return Forbid(); }
    }

    [HttpDelete("comments/{id}")]
    [Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
    [EnableRateLimiting("public-blog-interactions")]
    public async Task<IActionResult> WithdrawComment(string slug, string id, CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request)) return StatusCode(StatusCodes.Status403Forbidden);
        var (channelId, post) = await ResolvePostAsync(slug, cancellationToken);
        if (post is null) return NotFound();
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId is null) return Unauthorized();
        try
        {
            var comment = await interactions.WithdrawCommentAsync(channelId!, id, accountId, cancellationToken);
            return comment is null || comment.PostId != post.Id ? NotFound() : NoContent();
        }
        catch (BlogInteractionForbiddenException) { return Forbid(); }
        catch (BlogInteractionConflictException exception) { return Conflict(new ProblemDetails { Title = exception.Message }); }
    }

    private async Task<(string? ChannelId, BlogPost? Post)> ResolvePostAsync(string slug, CancellationToken cancellationToken)
    {
        var configured = configuration["YouTubeChannelId"]?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            logger.LogError("Public blog interactions are missing YouTubeChannelId configuration");
            return (null, null);
        }
        var channelId = (await content.GetChannelByIdAsync(configured))?.ChannelId;
        if (string.IsNullOrWhiteSpace(channelId))
        {
            logger.LogError("Public blog interactions cannot resolve configured channel {ConfiguredChannelId}", configured);
            return (null, null);
        }
        return (channelId, await blogService.GetPublishedAsync(channelId, slug));
    }
}
