using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT authentication
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        IApiKeyRepository apiKeyRepository,
        IApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        // Check if name already exists
        var existingKey = await _apiKeyRepository.GetByNameAsync(request.Name);
        if (existingKey != null)
        {
            return Conflict(new { message = "An API key with this name already exists" });
        }

        var (apiKey, unhashedKey) = await _apiKeyService.CreateApiKeyAsync(
            request.Name,
            request.Description ?? string.Empty,
            request.RateLimitPerMinute,
            request.AllowedIpAddresses,
            request.ExpiresAt
        );

        _logger.LogInformation("API key created: {KeyName} by user {User}", request.Name, User.Identity?.Name);

        return Ok(new CreateApiKeyResponse
        {
            Id = apiKey.Id!,
            Name = apiKey.Name,
            Description = apiKey.Description,
            Key = unhashedKey, // Only returned once
            RateLimitPerMinute = apiKey.RateLimitPerMinute,
            AllowedIpAddresses = apiKey.AllowedIpAddresses,
            ExpiresAt = apiKey.ExpiresAt,
            CreatedAt = apiKey.CreationDateTime,
            Message = "IMPORTANT: Save this key securely. It will not be shown again."
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllApiKeys()
    {
        var apiKeys = await _apiKeyRepository.GetItemsAsync();
        
        var response = apiKeys.Select(k => new ApiKeyDto
        {
            Id = k.Id!,
            Name = k.Name,
            Description = k.Description,
            IsActive = k.IsActive,
            RateLimitPerMinute = k.RateLimitPerMinute,
            AllowedIpAddresses = k.AllowedIpAddresses,
            LastUsedAt = k.LastUsedAt,
            ExpiresAt = k.ExpiresAt,
            CreatedAt = k.CreationDateTime
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApiKey(string id)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null)
        {
            return NotFound(new { message = "API key not found" });
        }

        return Ok(new ApiKeyDto
        {
            Id = apiKey.Id!,
            Name = apiKey.Name,
            Description = apiKey.Description,
            IsActive = apiKey.IsActive,
            RateLimitPerMinute = apiKey.RateLimitPerMinute,
            AllowedIpAddresses = apiKey.AllowedIpAddresses,
            LastUsedAt = apiKey.LastUsedAt,
            ExpiresAt = apiKey.ExpiresAt,
            CreatedAt = apiKey.CreationDateTime
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiKey(string id, [FromBody] UpdateApiKeyRequest request)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null)
        {
            return NotFound(new { message = "API key not found" });
        }

        var updatedKey = apiKey with
        {
            Name = request.Name ?? apiKey.Name,
            Description = request.Description ?? apiKey.Description,
            RateLimitPerMinute = request.RateLimitPerMinute ?? apiKey.RateLimitPerMinute,
            AllowedIpAddresses = request.AllowedIpAddresses ?? apiKey.AllowedIpAddresses,
            ExpiresAt = request.ExpiresAt ?? apiKey.ExpiresAt
        };

        await _apiKeyRepository.UpdateItemAsync(updatedKey);

        _logger.LogInformation("API key updated: {KeyName} by user {User}", updatedKey.Name, User.Identity?.Name);

        return Ok(new { message = "API key updated successfully" });
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> ToggleApiKey(string id)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null)
        {
            return NotFound(new { message = "API key not found" });
        }

        var updatedKey = apiKey with { IsActive = !apiKey.IsActive };
        await _apiKeyRepository.UpdateItemAsync(updatedKey);

        var status = updatedKey.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("API key {Status}: {KeyName} by user {User}", status, updatedKey.Name, User.Identity?.Name);

        return Ok(new { 
            message = $"API key {status} successfully",
            isActive = updatedKey.IsActive
        });
    }

    [HttpPost("{id}/regenerate")]
    public async Task<IActionResult> RegenerateApiKey(string id)
    {
        var oldApiKey = await _apiKeyRepository.GetItemAsync(id);
        if (oldApiKey == null)
        {
            return NotFound(new { message = "API key not found" });
        }

        // Generate new key
        var newUnhashedKey = _apiKeyService.GenerateApiKey();
        var newHashedKey = _apiKeyService.HashApiKey(newUnhashedKey);

        var updatedKey = oldApiKey with
        {
            Key = newHashedKey,
            LastUsedAt = null // Reset last used
        };

        await _apiKeyRepository.UpdateItemAsync(updatedKey);

        _logger.LogInformation("API key regenerated: {KeyName} by user {User}", updatedKey.Name, User.Identity?.Name);

        return Ok(new
        {
            message = "API key regenerated successfully",
            key = newUnhashedKey,
            warning = "IMPORTANT: Save this key securely. It will not be shown again."
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiKey(string id)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null)
        {
            return NotFound(new { message = "API key not found" });
        }

        await _apiKeyRepository.DeleteItemAsync(id);

        _logger.LogInformation("API key deleted: {KeyName} by user {User}", apiKey.Name, User.Identity?.Name);

        return Ok(new { message = "API key deleted successfully" });
    }
}

// DTOs
public record CreateApiKeyRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? RateLimitPerMinute { get; init; }
    public List<string>? AllowedIpAddresses { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record CreateApiKeyResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int RateLimitPerMinute { get; init; }
    public List<string> AllowedIpAddresses { get; init; } = new();
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record UpdateApiKeyRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? RateLimitPerMinute { get; init; }
    public List<string>? AllowedIpAddresses { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record ApiKeyDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int RateLimitPerMinute { get; init; }
    public List<string> AllowedIpAddresses { get; init; } = new();
    public DateTime? LastUsedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}