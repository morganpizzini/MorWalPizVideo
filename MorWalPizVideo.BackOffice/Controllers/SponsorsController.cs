using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Domain;
using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.Models.Configuration;

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

public class SponsorsController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly IBlobService _blobService;
    private readonly BlobStorageOptions _blobOptions;

    public SponsorsController(DataService dataService, IBlobService blobService,BlobStorageOptions blobOptions)
    {
        _dataService = dataService;
        _blobService = blobService;
        _blobOptions = blobOptions;
    }

    [HttpGet]
    public async Task<IActionResult> GetSponsors()
    {
        var entities = await _dataService.GetSponsors();
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSponsor(string id)
    {
        var entity = await _dataService.GetSponsorById(id);
        if (entity == null)
            return NotFound();
        return Ok(entity);
    }

    [HttpPost]
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
        await _dataService.SaveSponsor(sponsor);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSponsor([FromRoute] string id, [FromForm] UpdateSponsorRequest request)
    {
        var entity = await _dataService.GetSponsorById(id);
        if (entity == null)
            return BadRequest("Sponsor not found");

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

        await _dataService.UpdateSponsor(updatedSponsor);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSponsor(BaseRequestId request)
    {
        var entity = await _dataService.GetSponsorById(request.Id);
        if (entity == null)
        {
            return BadRequest("Sponsor not found");
        }

        await _dataService.DeleteSponsor(entity.Id);
        return NoContent();
    }
}
