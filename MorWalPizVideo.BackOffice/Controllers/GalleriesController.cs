using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/galleries")]
[RequireChannelScope]
public sealed class GalleriesController(IGalleryRepository repository, IBlobService blobService) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ImagesView, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<ActionResult<IReadOnlyList<GalleryContract>>> GetAll()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var items = await repository.GetItemsAsync(item => item.ChannelId == channelId);
        return Ok(items.OrderByDescending(item => item.CreationDateTime).Select(ContractUtils.Convert).ToArray());
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ImagesView, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<ActionResult<GalleryContract>> Get(string id)
    {
        var item = (await repository.GetItemsAsync(item => item.Id == id && item.ChannelId == HttpContext.GetChannelContext().ChannelId)).FirstOrDefault();
        return item is null ? NotFound() : Ok(ContractUtils.Convert(item));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ImagesCreate, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<ActionResult<GalleryContract>> Create(GalleryRequest request)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(validation);
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var normalizedSlug = Gallery.NormalizeSlug(request.Slug);
        if (await repository.GetBySlugAsync(channelId, normalizedSlug) is not null)
            return Conflict("A gallery with this slug already exists for the selected channel.");
        var item = await repository.AddItemAsync(ToEntity(request, channelId));
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ContractUtils.Convert(item));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ImagesCreate, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<ActionResult<GalleryContract>> Update(string id, GalleryRequest request)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(validation);
        var current = (await repository.GetItemsAsync(item => item.Id == id && item.ChannelId == HttpContext.GetChannelContext().ChannelId)).FirstOrDefault();
        if (current is null) return NotFound();
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var normalizedSlug = Gallery.NormalizeSlug(request.Slug);
        if (await repository.GetBySlugAsync(channelId, normalizedSlug) is { } duplicate && duplicate.Id != current.Id)
            return Conflict("A gallery with this slug already exists for the selected channel.");
        var item = current with { Name = request.Name.Trim(), Description = request.Description?.Trim(), Slug = normalizedSlug, Expiration = DateTime.UtcNow.AddDays(request.ExpirationDays) };
        await repository.UpdateItemAsync(item);
        return Ok(ContractUtils.Convert(item));
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ImagesDelete, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<IActionResult> Delete(string id)
    {
        var item = (await repository.GetItemsAsync(entity => entity.Id == id && entity.ChannelId == HttpContext.GetChannelContext().ChannelId)).FirstOrDefault();
        if (item is null) return NotFound();
        foreach (var image in item.BlobReference)
        {
            await blobService.DeleteImageAsync(image.StorageKey);
            if (!string.IsNullOrWhiteSpace(image.PreviewStorageKey))
                await blobService.DeleteImageAsync(image.PreviewStorageKey);
        }
        await repository.DeleteItemAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/images")]
    [AllowUser(AuthorizationPermissionKeys.ImagesCreate, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<ActionResult<GalleryContract>> UploadImages(string id, IFormFileCollection files)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = (await repository.GetItemsAsync(item => item.Id == id && item.ChannelId == channelId)).FirstOrDefault();
        if (existing is null) return NotFound();
        if (files.Count == 0) return BadRequest("At least one image is required.");
        var images = existing.BlobReference.ToList();
        var uploadedKeys = new List<string>();
        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                foreach (var uploadedKey in uploadedKeys)
                    await blobService.DeleteImageAsync(uploadedKey);
                return BadRequest("Empty images are not allowed.");
            }
            try
            {
                await using var input = file.OpenReadStream();
                var prepared = await ChannelNewsMediaProcessor.PrepareImageAsIsAsync(input);
                var storageKey = $"galleries/{channelId}/{existing.Id}/{Guid.NewGuid():N}{prepared.Extension}";
                await blobService.UploadImagesAsync(storageKey, prepared.Content, false, HttpContext.RequestAborted);
                uploadedKeys.Add(storageKey);
                prepared.Content.Position = 0;
                var preview = await ChannelNewsMediaProcessor.PrepareImageAsync(prepared.Content);
                var previewKey = $"galleries/{channelId}/{existing.Id}/{Guid.NewGuid():N}-preview{preview.Extension}";
                await blobService.UploadImagesAsync(previewKey, preview.Content, false, HttpContext.RequestAborted);
                uploadedKeys.Add(previewKey);
                images.Add(new GalleryImage { StorageKey = storageKey, PublicUrl = blobService.GetImageUrl(storageKey), PreviewStorageKey = previewKey, PreviewPublicUrl = blobService.GetImageUrl(previewKey), ContentType = prepared.ContentType, Width = prepared.Width, Height = prepared.Height, DisplayOrder = images.Count });
            }
            catch (Exception exception)
            {
                foreach (var uploadedKey in uploadedKeys)
                    await blobService.DeleteImageAsync(uploadedKey);
                return BadRequest($"Image '{file.FileName}' is not a valid decodable image: {exception.Message}");
            }
        }
        try
        {
            var updated = existing with { BlobReference = images };
            await repository.UpdateItemAsync(updated);
            return Ok(ContractUtils.Convert(updated));
        }
        catch
        {
            foreach (var uploadedKey in uploadedKeys)
                await blobService.DeleteImageAsync(uploadedKey);
            throw;
        }
    }

    [HttpDelete("{id}/images/{imageIndex:int}")]
    [AllowUser(AuthorizationPermissionKeys.ImagesDelete, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<ActionResult<GalleryContract>> DeleteImage(string id, int imageIndex)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existing = (await repository.GetItemsAsync(item => item.Id == id && item.ChannelId == channelId)).FirstOrDefault();
        if (existing is null || imageIndex < 0 || imageIndex >= existing.BlobReference.Count) return NotFound();
        var image = existing.BlobReference[imageIndex];
        var updated = existing with { BlobReference = existing.BlobReference.Where((_, index) => index != imageIndex).Select((item, index) => item with { DisplayOrder = index }).ToArray() };
        await repository.UpdateItemAsync(updated);
        await blobService.DeleteImageAsync(image.StorageKey);
        if (!string.IsNullOrWhiteSpace(image.PreviewStorageKey))
            await blobService.DeleteImageAsync(image.PreviewStorageKey);
        return Ok(ContractUtils.Convert(updated));
    }

    private static Gallery ToEntity(GalleryRequest request, string channelId) => new()
    {
        ChannelId = channelId,
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
        Slug = Gallery.NormalizeSlug(request.Slug),
        Expiration = DateTime.UtcNow.AddDays(request.ExpirationDays)
    };

    private static string? Validate(GalleryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug)) return "Name and slug are required.";
        return Gallery.IsAllowedExpirationDays(request.ExpirationDays) ? null : "ExpirationDays must be 3, 7, or 21.";
    }
}

public sealed class GalleryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int ExpirationDays { get; set; } = 7;
}