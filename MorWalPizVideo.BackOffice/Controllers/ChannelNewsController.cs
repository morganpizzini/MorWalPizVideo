using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class ChannelNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ChannelNewsStatus Status { get; set; } = ChannelNewsStatus.Draft;
    public DateTime? PublicationTimeUtc { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class ChannelNewsStatusRequest
{
    public ChannelNewsStatus Status { get; set; }
    public DateTime? PublicationTimeUtc { get; set; }
}

[Route("api/[controller]")]
[ApiController]
[RequireChannelScope]
public sealed class ChannelNewsController(
    IChannelNewsService channelNewsService,
    IBlobService blobService,
    ICrossApiService crossApiService) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsView, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<ActionResult<IReadOnlyList<ChannelNewsContract>>> GetAll()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var items = await channelNewsService.GetForChannelAsync(channelId);
        return Ok(items.OrderByDescending(item => item.UpdatedDateTime).Select(ContractUtils.Convert).ToArray());
    }

    [HttpGet("{idOrSlug}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsView, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<ActionResult<ChannelNewsContract>> Get(string idOrSlug)
    {
        var item = await channelNewsService.GetByIdOrSlugAsync(idOrSlug, HttpContext.GetChannelContext().ChannelId);
        return item is null ? NotFound() : Ok(ContractUtils.Convert(item));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsCreate, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<ActionResult<ChannelNewsContract>> Create(ChannelNewsRequest request)
    {
        var validation = Validate(request);
        if (validation is not null)
            return BadRequest(validation);

        var item = await channelNewsService.CreateAsync(ToEntity(request, HttpContext.GetChannelContext().ChannelId));
        await InvalidateAsync();
        return CreatedAtAction(nameof(Get), new { idOrSlug = item.Id }, ContractUtils.Convert(item));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsUpdate, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<ActionResult<ChannelNewsContract>> Update(string id, ChannelNewsRequest request)
    {
        var validation = Validate(request);
        if (validation is not null)
            return BadRequest(validation);

        var channelId = HttpContext.GetChannelContext().ChannelId;
        if (await channelNewsService.GetByIdOrSlugAsync(id, channelId) is null)
            return NotFound();

        var item = await channelNewsService.UpdateAsync(ToEntity(request, channelId) with { Id = id }, channelId);
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(item!));
    }

    [HttpPost("{id}/status")]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsUpdate, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<ActionResult<ChannelNewsContract>> UpdateStatus(string id, ChannelNewsStatusRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = await channelNewsService.GetByIdOrSlugAsync(id, channelId);
        if (existing is null)
            return NotFound();
        if (!Enum.IsDefined(request.Status))
            return BadRequest("Status is invalid.");

        if (request.Status == ChannelNewsStatus.Scheduled && request.PublicationTimeUtc is null)
            return BadRequest("PublicationTimeUtc is required for scheduled ChannelNews.");

        var item = await channelNewsService.UpdateAsync(existing with
        {
            Status = request.Status,
            PublicationTimeUtc = request.PublicationTimeUtc ?? existing.PublicationTimeUtc
        }, channelId);
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(item!));
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsDelete, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await channelNewsService.DeleteAsync(id, HttpContext.GetChannelContext().ChannelId);
        if (!deleted)
            return NotFound();

        await InvalidateAsync();
        return NoContent();
    }

    [HttpPost("{id}/images")]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsUpdate, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<ActionResult<ChannelNewsContract>> UploadImages(string id, IFormFileCollection files)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = await channelNewsService.GetByIdOrSlugAsync(id, channelId);
        if (existing is null)
            return NotFound();
        if (files.Count == 0)
            return BadRequest("At least one image is required.");
        if (existing.Images.Count + files.Count > 10)
            return BadRequest("A ChannelNews item can contain at most 10 images.");

        var images = existing.Images.ToList();
        foreach (var file in files)
        {
            if (file.Length == 0)
                return BadRequest("Empty images are not allowed.");

            try
            {
                await using var input = file.OpenReadStream();
                var prepared = await ChannelNewsMediaProcessor.PrepareImageAsync(input);
                var storageKey = $"channelnews/{channelId}/{existing.Id}/{Guid.NewGuid():N}{prepared.Extension}";
                await blobService.UploadImagesAsync(storageKey, prepared.Content, false);
                images.Add(new ChannelNewsImage
                {
                    StorageKey = storageKey,
                    PublicUrl = blobService.GetImageUrl(storageKey),
                    ContentType = prepared.ContentType,
                    Width = prepared.Width,
                    Height = prepared.Height,
                    AltText = Path.GetFileNameWithoutExtension(file.FileName)
                });
            }
            catch (Exception exception)
            {
                return BadRequest($"Image '{file.FileName}' is not a valid decodable image: {exception.Message}");
            }
        }

        var updated = await channelNewsService.UpdateAsync(existing with { Images = images }, channelId);
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(updated!));
    }

    [HttpDelete("{id}/images/{imageIndex:int}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelNewsUpdate, AuthorizationPermissionKeys.ChannelNewsManage)]
    public async Task<ActionResult<ChannelNewsContract>> DeleteImage(string id, int imageIndex)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = await channelNewsService.GetByIdOrSlugAsync(id, channelId);
        if (existing is null)
            return NotFound();
        if (imageIndex < 0 || imageIndex >= existing.Images.Count)
            return NotFound();

        var image = existing.Images[imageIndex];
        var updated = await channelNewsService.UpdateAsync(
            existing with { Images = existing.Images.Where((_, index) => index != imageIndex).ToArray() },
            channelId);
        if (updated is null)
            return NotFound();

        await blobService.DeleteImageAsync(image.StorageKey);
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(updated));
    }

    private static ChannelNews ToEntity(ChannelNewsRequest request, string channelId) => new()
    {
        ChannelId = channelId,
        Title = request.Title,
        Subtitle = request.Subtitle,
        DescriptionHtml = request.DescriptionHtml,
        Slug = request.Slug,
        Status = request.Status,
        PublicationTimeUtc = request.PublicationTimeUtc,
        DisplayOrder = request.DisplayOrder
    };

    private static string? Validate(ChannelNewsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return "Title is required.";
        if (!Enum.IsDefined(request.Status))
            return "Status is invalid.";
        if (request.Status == ChannelNewsStatus.Scheduled && request.PublicationTimeUtc is null)
            return "PublicationTimeUtc is required for scheduled ChannelNews.";
        return null;
    }

    private async Task InvalidateAsync()
    {
        await crossApiService.ResetCache(CacheKeys.ChannelNews);
        await crossApiService.PurgeCache(ApiTagCacheKeys.ChannelNews);
    }
}
