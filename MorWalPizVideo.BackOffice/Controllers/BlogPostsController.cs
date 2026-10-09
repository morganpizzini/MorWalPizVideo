using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using SixLabors.ImageSharp;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/blogposts")]
[RequireChannelScope]
[RequestSizeLimit(512_000)]
public sealed class BlogPostsController(BlogService service, IBlobService blobs, IOptions<BlobStorageOptions> options,
    ICrossApiService crossApi, ILogger<BlogPostsController> logger) : ControllerBase
{
    private string ChannelId => HttpContext.GetChannelContext().ChannelId;

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.PagesView, AuthorizationPermissionKeys.PagesManage)]
    public async Task<IActionResult> List([FromQuery] int page = 1)
    {
        if (page is < 1 or > 10000) return BadRequest("Invalid page.");
        return Ok((await service.ListAsync(ChannelId, false, page, 50)).Select(BlogPostResponse.From));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PagesView, AuthorizationPermissionKeys.PagesManage)]
    public async Task<IActionResult> Get(string id)
    {
        var post = await service.GetAsync(ChannelId, id);
        return post is null ? NotFound() : Ok(BlogPostResponse.From(post));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.PagesCreate, AuthorizationPermissionKeys.PagesManage)]
    public async Task<IActionResult> Create(SaveBlogPostRequest request)
    {
        try
        {
            var post = await service.CreateAsync(ChannelId, request.Slug, request.Draft);
            return CreatedAtAction(nameof(Get), new { id = post.Id }, BlogPostResponse.From(post));
        }
        catch (BlogValidationException exception) { return BadRequest(exception.Message); }
        catch (BlogConflictException exception) { return Conflict(exception.Message); }
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PagesUpdate, AuthorizationPermissionKeys.PagesManage)]
    public Task<IActionResult> Save(string id, SaveBlogPostRequest request) => MutateAsync(() =>
        service.SaveAsync(ChannelId, id, request.Revision, request.Slug, request.Draft), false);

    [HttpPost("{id}/publish")]
    [AllowUser(AuthorizationPermissionKeys.PagesManage)]
    public Task<IActionResult> Publish(string id, BlogRevisionRequest request) => MutateAsync(() =>
        service.PublishAsync(ChannelId, id, request.Revision, true), true);

    [HttpPost("{id}/unpublish")]
    [AllowUser(AuthorizationPermissionKeys.PagesManage)]
    public Task<IActionResult> Unpublish(string id, BlogRevisionRequest request) => MutateAsync(() =>
        service.PublishAsync(ChannelId, id, request.Revision, false), true);

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PagesDelete, AuthorizationPermissionKeys.PagesManage)]
    public async Task<IActionResult> Delete(string id, [FromQuery] long revision)
    {
        var result = await MutateAsync(() => service.DeleteAsync(ChannelId, id, revision), true);
        return result is OkObjectResult ? NoContent() : result;
    }

    [HttpPost("{id}/images")]
    [RequestSizeLimit(12_000_000)]
    [AllowUser(AuthorizationPermissionKeys.PagesUpdate, AuthorizationPermissionKeys.PagesManage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(string id, [FromForm] BlogPostImageUploadRequest request)
    {
        if (request.File.Length is <= 0 or > 10_000_000 || string.IsNullOrWhiteSpace(request.AltText) || request.AltText.Length > 300 || request.Revision < 1)
            return BadRequest("Image and alt text (1-300 characters) are required.");
        var existing = await service.GetAsync(ChannelId, id);
        if (existing is null) return NotFound();
        if (existing.Revision != request.Revision) return Conflict("Post changed. Reload before uploading.");
        var storageKey = $"blog/{ChannelId}/{id}/{Guid.NewGuid():N}.jpg";
        var uploaded = false;
        try
        {
            await using var input = request.File.OpenReadStream();
            var info = await Image.IdentifyAsync(input);
            if ((long)info.Width * info.Height > 25_000_000 || info.FrameMetadataCollection.Count > 1)
                return BadRequest("Use a single-frame image of at most 25 megapixels.");
            input.Position = 0;
            var prepared = await ChannelNewsMediaProcessor.PrepareImageAsync(input);
            await using var preparedContent = prepared.Content;
            storageKey = $"blog/{ChannelId}/{id}/{Guid.NewGuid():N}{prepared.Extension}";
            await blobs.UploadImageAsync(storageKey, prepared.Content, options.Value.PageContainerName);
            uploaded = true;
            var post = await service.AddImageAsync(ChannelId, id, request.Revision, new PageImage
            {
                StorageKey = storageKey, PublicUrl = blobs.GetImageUrl(storageKey, options.Value.PageContainerName),
                ContentType = prepared.ContentType, Width = prepared.Width, Height = prepared.Height, AltText = request.AltText.Trim()
            });
            if (post is null)
            {
                await blobs.DeleteImageAsync(storageKey, options.Value.PageContainerName);
                return NotFound();
            }
            return Ok(BlogPostResponse.From(post));
        }
        catch (UnknownImageFormatException) { return BadRequest("Unsupported image format."); }
        catch (InvalidImageContentException) { return BadRequest("Invalid image content."); }
        catch (BlogConflictException exception)
        {
            if (uploaded) await blobs.DeleteImageAsync(storageKey, options.Value.PageContainerName);
            return Conflict(exception.Message);
        }
        catch (BlogValidationException exception)
        {
            if (uploaded) await blobs.DeleteImageAsync(storageKey, options.Value.PageContainerName);
            return BadRequest(exception.Message);
        }
        catch
        {
            if (uploaded) await blobs.DeleteImageAsync(storageKey, options.Value.PageContainerName);
            throw;
        }
    }

    public sealed class BlogPostImageUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public long Revision { get; set; }
        public string AltText { get; set; } = string.Empty;
    }

    private async Task<IActionResult> MutateAsync(Func<Task<BlogPost?>> operation, bool invalidate)
    {
        try
        {
            var post = await operation();
            if (post is null) return NotFound();
            if (invalidate)
            {
                await EvictAsync(() => crossApi.PurgeCache(ApiTagCacheKeys.Blog), post);
                await EvictAsync(() => crossApi.ResetCache(CacheKeys.Blog), post);
            }
            return Ok(BlogPostResponse.From(post));
        }
        catch (BlogValidationException exception) { return BadRequest(exception.Message); }
        catch (BlogConflictException exception) { return Conflict(exception.Message); }
    }

    private async Task EvictAsync(Func<Task<string>> operation, BlogPost post)
    {
        try { await operation().WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Blog cache eviction failed after persistence for {PostId} in {ChannelId}; TTL is 300 seconds", post.Id, ChannelId);
            Response.Headers["X-Blog-Cache-Warning"] = "Eviction failed; public cache expires within 300 seconds";
        }
    }
}