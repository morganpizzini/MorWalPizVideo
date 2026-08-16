using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class PageRequest
{
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public PageStatus Status { get; set; } = PageStatus.Draft;
}

[Route("api/[controller]")]
[ApiController]
[RequireChannelScope]
public sealed class PagesController(IPageService pageService, IBlobService blobService, ICrossApiService crossApiService, IOptions<BlobStorageOptions> blobOptions) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.PagesView, AuthorizationPermissionKeys.PagesManage)]
    public async Task<ActionResult<IReadOnlyList<PageContract>>> GetAll()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var pages = await pageService.GetForChannelAsync(channelId);
        return Ok(pages.OrderBy(page => page.Title).Select(ContractUtils.Convert).ToArray());
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PagesView, AuthorizationPermissionKeys.PagesManage)]
    public async Task<ActionResult<PageContract>> Get(string id)
    {
        var page = await pageService.GetByIdAsync(id, HttpContext.GetChannelContext().ChannelId);
        return page is null ? NotFound() : Ok(ContractUtils.Convert(page));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.PagesCreate, AuthorizationPermissionKeys.PagesManage)]
    public async Task<ActionResult<PageContract>> Create(PageRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var page = ToEntity(request, channelId);
        var errors = pageService.Validate(page);
        if (errors.Count > 0) return BadRequest(errors);
        if (!await pageService.IsUrlAvailableAsync(page.Url, channelId))
            return Conflict("A page with this URL already exists.");

        Page created;
        try
        {
            created = await pageService.CreateAsync(page);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            return Conflict("A page with this URL already exists.");
        }
        await InvalidateAsync();
        return CreatedAtAction(nameof(Get), new { id = created.Id }, ContractUtils.Convert(created));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PagesUpdate, AuthorizationPermissionKeys.PagesManage)]
    public async Task<ActionResult<PageContract>> Update(string id, PageRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = await pageService.GetByIdAsync(id, channelId);
        if (existing is null) return NotFound();

        var page = ToEntity(request, channelId) with
        {
            Id = existing.Id,
            InlineImages = existing.InlineImages
        };
        var errors = pageService.Validate(page);
        if (errors.Count > 0) return BadRequest(errors);
        if (!await pageService.IsUrlAvailableAsync(page.Url, channelId, id))
            return Conflict("A page with this URL already exists.");

        Page? updated;
        try
        {
            updated = await pageService.UpdateAsync(page, channelId);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            return Conflict("A page with this URL already exists.");
        }
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(updated!));
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PagesDelete, AuthorizationPermissionKeys.PagesManage)]
    public async Task<IActionResult> Delete(string id)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        if (!await pageService.DeleteAsync(id, channelId)) return NotFound();
        await InvalidateAsync();
        return NoContent();
    }

    [HttpPost("{id}/images")]
    [AllowUser(AuthorizationPermissionKeys.PagesUpdate, AuthorizationPermissionKeys.PagesManage)]
    public async Task<ActionResult<IReadOnlyList<PageImageContract>>> UploadImages(string id, IFormFileCollection files)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = await pageService.GetByIdAsync(id, channelId);
        if (existing is null) return NotFound();
        if (files.Count == 0) return BadRequest("At least one image is required.");

        var images = existing.InlineImages.ToList();
        var uploadedStorageKeys = new List<string>();
        try
        {
            foreach (var file in files)
            {
                if (file.Length == 0) return BadRequest("Empty images are not allowed.");
                await using var input = file.OpenReadStream();
                var prepared = await ChannelNewsMediaProcessor.PrepareImageAsync(input);
                var storageKey = $"pages/{channelId}/{existing.Id}/{Guid.NewGuid():N}{prepared.Extension}";
                await blobService.UploadImageAsync(storageKey, prepared.Content, blobOptions.Value.PageContainerName);
                uploadedStorageKeys.Add(storageKey);
                images.Add(new PageImage
                {
                    StorageKey = storageKey,
                    PublicUrl = blobService.GetImageUrl(storageKey, blobOptions.Value.PageContainerName),
                    ContentType = prepared.ContentType,
                    Width = prepared.Width,
                    Height = prepared.Height,
                    AltText = Path.GetFileNameWithoutExtension(file.FileName)
                });
            }
        }
        catch (Exception exception)
        {
            foreach (var storageKey in uploadedStorageKeys)
                await blobService.DeleteImageAsync(storageKey, blobOptions.Value.PageContainerName);
            return BadRequest($"Unable to upload page images: {exception.Message}");
        }

        Page? updated;
        try
        {
            updated = await pageService.UpdateAsync(existing with { InlineImages = images }, channelId);
        }
        catch
        {
            foreach (var storageKey in uploadedStorageKeys)
                await blobService.DeleteImageAsync(storageKey, blobOptions.Value.PageContainerName);
            throw;
        }
        if (updated is null) return NotFound();
        await InvalidateAsync();
        return Ok(updated!.InlineImages.Select(ContractUtils.Convert).ToArray());
    }

    [HttpDelete("{id}/images/{imageIndex:int}")]
    [AllowUser(AuthorizationPermissionKeys.PagesUpdate, AuthorizationPermissionKeys.PagesManage)]
    public async Task<ActionResult<IReadOnlyList<PageImageContract>>> DeleteImage(string id, int imageIndex)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = await pageService.GetByIdAsync(id, channelId);
        if (existing is null || imageIndex < 0 || imageIndex >= existing.InlineImages.Count) return NotFound();

        var image = existing.InlineImages[imageIndex];
        var updated = await pageService.UpdateAsync(
            existing with { InlineImages = existing.InlineImages.Where((_, index) => index != imageIndex).ToArray() },
            channelId);
        await blobService.DeleteImageAsync(image.StorageKey, blobOptions.Value.PageContainerName);
        await InvalidateAsync();
        return Ok(updated!.InlineImages.Select(ContractUtils.Convert).ToArray());
    }

    private static Page ToEntity(PageRequest request, string channelId) => new(
        ThumbnailUrl: request.ThumbnailUrl,
        Title: request.Title,
        Content: request.Content,
        Url: request.Url,
        VideoId: request.VideoId)
    {
        ChannelId = channelId,
        Status = request.Status
    };

    private async Task InvalidateAsync()
    {
        await crossApiService.ResetCache(CacheKeys.Pages);
        await crossApiService.PurgeCache(ApiTagCacheKeys.Pages);
        await crossApiService.ResetCache(CacheKeys.Navigation);
        await crossApiService.PurgeCache(ApiTagCacheKeys.Navigation);
    }
}
