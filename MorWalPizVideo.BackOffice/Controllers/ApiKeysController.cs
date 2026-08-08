using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT authentication; BackOffice-only API-key administration
[BlockImpersonation]
[RequireChannelScope]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;
    private readonly IYTChannelRepository _channelRepository;
    private readonly IVideoAuthorizationService _videoAuthorization;

    public ApiKeysController(
        IApiKeyRepository apiKeyRepository,
        IApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger,
        IYTChannelRepository channelRepository,
        IVideoAuthorizationService videoAuthorization)
    {
        _apiKeyRepository = apiKeyRepository;
        _apiKeyService = apiKeyService;
        _logger = logger;
        _channelRepository = channelRepository;
        _videoAuthorization = videoAuthorization;
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ApiKeysCreate, AuthorizationPermissionKeys.ApiKeysManage)]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        var channelId = HttpContext.GetChannelContext().ChannelId;

        // Check if name already exists
        var existingKey = await _apiKeyRepository.GetByNameAsync(request.Name);
        if (existingKey != null)
        {
            return Conflict(new { message = "An API key with this name already exists" });
        }

        var (apiKey, unhashedKey) = await _apiKeyService.CreateApiKeyAsync(
            request.Name,
            request.Description ?? string.Empty,
            channelId,
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
            ChannelId = apiKey.ChannelId,
            Message = "IMPORTANT: Save this key securely. It will not be shown again."
        });
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ApiKeysView, AuthorizationPermissionKeys.ApiKeysManage)]
    public async Task<IActionResult> GetAllApiKeys()
    {
        var channelContext = HttpContext.GetChannelContext();
        var apiKeys = await _apiKeyRepository.GetItemsAsync(key =>
            key.ChannelId == channelContext.ChannelId ||
            (channelContext.IsAdmin && string.IsNullOrWhiteSpace(key.ChannelId)));
        
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
            CreatedAt = k.CreationDateTime,
            ChannelId = k.ChannelId
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ApiKeysView, AuthorizationPermissionKeys.ApiKeysManage)]
    public async Task<IActionResult> GetApiKey(string id)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null || !CanAccessApiKey(apiKey))
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
            CreatedAt = apiKey.CreationDateTime,
            ChannelId = apiKey.ChannelId
        });
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ApiKeysUpdate, AuthorizationPermissionKeys.ApiKeysManage)]
    public async Task<IActionResult> UpdateApiKey(string id, [FromBody] UpdateApiKeyRequest request)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null || !CanAccessApiKey(apiKey))
        {
            return NotFound(new { message = "API key not found" });
        }

        var channelContext = HttpContext.GetChannelContext();
        var targetChannelId = apiKey.ChannelId;
        if (request.ChannelId is not null)
        {
            targetChannelId = request.ChannelId.Trim();
            if (string.IsNullOrWhiteSpace(targetChannelId))
            {
                return BadRequest(new { message = "ChannelId cannot be empty" });
            }

            if ((await _channelRepository.GetItemsAsync(channel => channel.ChannelId == targetChannelId)).Count == 0)
            {
                return NotFound(new { message = "Channel not found" });
            }

            if (!channelContext.IsAdmin && targetChannelId != channelContext.ChannelId)
            {
                return NotFound(new { message = "Channel not found" });
            }
        }

        var updatedKey = apiKey with
        {
            Name = request.Name ?? apiKey.Name,
            Description = request.Description ?? apiKey.Description,
            RateLimitPerMinute = request.RateLimitPerMinute ?? apiKey.RateLimitPerMinute,
            AllowedIpAddresses = request.AllowedIpAddresses ?? apiKey.AllowedIpAddresses,
            ExpiresAt = request.ExpiresAt ?? apiKey.ExpiresAt,
            ChannelId = targetChannelId
        };

        await _apiKeyRepository.UpdateItemAsync(updatedKey);

        _logger.LogInformation("API key updated: {KeyName} by user {User}", updatedKey.Name, User.Identity?.Name);

        return Ok(new { message = "API key updated successfully" });
    }

    [HttpPost("{id}/toggle")]
    [AllowUser(AuthorizationPermissionKeys.ApiKeysUpdate, AuthorizationPermissionKeys.ApiKeysManage)]
    public async Task<IActionResult> ToggleApiKey(string id)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null || !CanAccessApiKey(apiKey))
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
    [AllowUser(AuthorizationPermissionKeys.ApiKeysUpdate, AuthorizationPermissionKeys.ApiKeysManage)]
    public async Task<IActionResult> RegenerateApiKey(string id)
    {
        var oldApiKey = await _apiKeyRepository.GetItemAsync(id);
        if (oldApiKey == null || !CanAccessApiKey(oldApiKey))
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
    [AllowUser(AuthorizationPermissionKeys.ApiKeysDelete, AuthorizationPermissionKeys.ApiKeysManage)]
    public async Task<IActionResult> DeleteApiKey(string id)
    {
        var apiKey = await _apiKeyRepository.GetItemAsync(id);
        if (apiKey == null || !CanAccessApiKey(apiKey))
        {
            return NotFound(new { message = "API key not found" });
        }

        await _apiKeyRepository.DeleteItemAsync(id);

        _logger.LogInformation("API key deleted: {KeyName} by user {User}", apiKey.Name, User.Identity?.Name);

        return Ok(new { message = "API key deleted successfully" });
    }

    private bool CanAccessApiKey(ApiKey apiKey)
    {
        var context = HttpContext.GetChannelContext();
        return context.IsAdmin || apiKey.ChannelId == context.ChannelId;
    }
}

// DTOs
public record CreateApiKeyRequest
{
    [Required]
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
    public string? ChannelId { get; init; }
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
    public string? ChannelId { get; init; }
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
    public string? ChannelId { get; init; }
}