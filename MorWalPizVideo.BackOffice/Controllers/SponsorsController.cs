using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Domain;
using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.Models.Configuration;
using Microsoft.Extensions.Options;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateSponsorRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    [Required]
    public IFormFile Image { get; set; } = null!;
}

public class UpdateSponsorRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    public IFormFile? Image { get; set; }
}

[RequireChannelScope]
public class SponsorsController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly IBlobService _blobService;
    private readonly BlobStorageOptions _blobOptions;
    private readonly ICrossApiService _crossApiService;

    public SponsorsController(DataService dataService, IBlobService blobService, IOptions<BlobStorageOptions> blobOptions, ICrossApiService crossApiService)
    {
        _dataService = dataService;
        _blobService = blobService;
        _blobOptions = blobOptions.Value;
        _crossApiService = crossApiService;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.SponsorsView, AuthorizationPermissionKeys.SponsorsManage)]
    public async Task<IActionResult> GetSponsors()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entities = await _dataService.GetSponsors(channelId);
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.SponsorsView, AuthorizationPermissionKeys.SponsorsManage)]
    public async Task<IActionResult> GetSponsor(string id)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = (await _dataService.GetSponsors(channelId)).FirstOrDefault(sponsor => sponsor.Id == id);
        if (entity == null)
            return NotFound();
        return Ok(ContractUtils.Convert(entity));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.SponsorsCreate, AuthorizationPermissionKeys.SponsorsManage)]
    public async Task<IActionResult> CreateSponsor([FromForm] CreateSponsorRequest request)
    {
        // Validate image
        if (request.Image == null || request.Image.Length == 0)
            return BadRequest("Image is required");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Invalid image format. Allowed formats: JPG, PNG, GIF, WEBP");

        if (request.Image.Length > 5 * 1024 * 1024) // 5MB limit
            return BadRequest("Image size must be less than 5MB");

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}{extension}";

        // Upload to blob storage
        using var stream = new MemoryStream();
        await request.Image.CopyToAsync(stream);
        stream.Position = 0;
        
        await _blobService.UploadImageAsync(fileName, stream, _blobOptions.SponsorContainerName);

        // Create sponsor with filename
        var sponsor = new Sponsor(request.Title, request.Url, fileName);
        var created = await _dataService.SaveSponsor(sponsor, HttpContext.GetChannelContext().ChannelId);
        if (created is null)
            return Conflict("A sponsor with this title already exists.");
        await InvalidateCachesAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.SponsorsUpdate, AuthorizationPermissionKeys.SponsorsManage)]
    public async Task<IActionResult> UpdateSponsor([FromRoute] string id, [FromForm] UpdateSponsorRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = (await _dataService.GetSponsors(channelId)).FirstOrDefault(sponsor => sponsor.Id == id);
        if (entity == null)
            return NotFound("Sponsor not found");

        var imgSrc = entity.ImgSrc;

        // If new image provided, upload it
        if (request.Image != null && request.Image.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid image format. Allowed formats: JPG, PNG, GIF, WEBP");

            if (request.Image.Length > 5 * 1024 * 1024) // 5MB limit
                return BadRequest("Image size must be less than 5MB");

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";

            // Upload to blob storage
            using var stream = new MemoryStream();
            await request.Image.CopyToAsync(stream);
            stream.Position = 0;
            
            await _blobService.UploadImageAsync(fileName, stream, _blobOptions.SponsorContainerName);
            
            imgSrc = fileName;
        }

        var updatedSponsor = entity with
        {
            Title = request.Title,
            Url = request.Url,
            ImgSrc = imgSrc
        };

        var updated = await _dataService.UpdateSponsor(updatedSponsor, channelId);
        if (updated is null)
            return NotFound("Sponsor not found");
        await InvalidateCachesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.SponsorsDelete, AuthorizationPermissionKeys.SponsorsManage)]
    public async Task<IActionResult> DeleteSponsor(BaseRequestId request)
    {
        var deleted = await _dataService.DeleteSponsor(request.Id, HttpContext.GetChannelContext().ChannelId);
        if (!deleted)
        {
            return NotFound("Sponsor not found");
        }

        await InvalidateCachesAsync();
        return NoContent();
    }

    private async Task InvalidateCachesAsync()
    {
        await _crossApiService.ResetCache(CacheKeys.Sponsors);
        await _crossApiService.PurgeCache(ApiTagCacheKeys.Sponsors);
        await _crossApiService.ResetCache(CacheKeys.ShortLinks);
        await _crossApiService.PurgeCache(ApiTagCacheKeys.ShortLinks);
    }
}
