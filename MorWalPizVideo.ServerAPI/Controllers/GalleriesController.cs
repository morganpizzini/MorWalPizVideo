using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers;

[AllowAnonymous]
public sealed class GalleriesController(IGalleryRepository repository, IBlobService blobService,IConfiguration _configuration, ILogger<GalleriesController> _logger) : ApplicationControllerBase
{
    private string? GetYouTubeChannelId()
        {
            var channelId = _configuration["YouTubeChannelId"]?.Trim();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                _logger.LogError("Public matches endpoint is missing the YouTubeChannelId configuration");
            }

            return channelId;
        }
    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        var channelId = GetYouTubeChannelId();
        if (string.IsNullOrWhiteSpace(channelId)) return BadRequest("X-Channel-Id is required.");
        var item = await repository.GetBySlugAsync(channelId, slug);
        if (item is null || item.Expiration <= DateTime.UtcNow) return NotFound();
        await repository.IncrementDownloadCountAsync(item.Id);
        return Ok(ContractUtils.ConvertPublic(item));
    }

    [HttpGet("{slug}/download")]
    public async Task<IActionResult> Download(string slug, [FromQuery] string? indexes = null)
    {
        var channelId = GetYouTubeChannelId();
        if (string.IsNullOrWhiteSpace(channelId)) return BadRequest("X-Channel-Id is required.");
        var item = await repository.GetBySlugAsync(channelId, slug);
        if (item is null || item.Expiration <= DateTime.UtcNow) return NotFound();
        var selected = ParseIndexes(indexes, item.BlobReference.Count);
        if (selected.Count == 0) return BadRequest("At least one image must be selected.");
        await repository.IncrementDownloadCountAsync(item.Id);
        if (selected.Count == 1)
        {
            var image = item.BlobReference[selected[0]];
            var stream = await blobService.DownloadImageAsync(image.StorageKey, false, HttpContext.RequestAborted);
            return stream is null ? NotFound() : File(stream, image.ContentType, Path.GetFileName(image.StorageKey));
        }

        var archive = new MemoryStream();
        using (var zip = new ZipArchive(archive, ZipArchiveMode.Create, true))
        {
            foreach (var index in selected)
            {
                var image = item.BlobReference[index];
                var stream = await blobService.DownloadImageAsync(image.StorageKey, false, HttpContext.RequestAborted);
                if (stream is null) continue;
                var entry = zip.CreateEntry(Path.GetFileName(image.StorageKey));
                await using var entryStream = entry.Open();
                await stream.CopyToAsync(entryStream, HttpContext.RequestAborted);
            }
        }
        archive.Position = 0;
        return File(archive, "application/zip", $"{item.Slug}.zip");
    }

    private static List<int> ParseIndexes(string? indexes, int count)
        => (indexes ?? string.Join(',', Enumerable.Range(0, count))).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var index) ? index : -1)
            .Where(index => index >= 0 && index < count).Distinct().ToList();
}