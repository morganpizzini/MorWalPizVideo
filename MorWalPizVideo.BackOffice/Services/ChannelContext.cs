using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public static class ChannelContextConstants
{
  public const string HeaderName = "X-Channel-Id";
  public const string ItemKey = "BackOffice.ChannelContext";
}

public sealed record ChannelContext(
    string ChannelId,
    YTChannel Channel,
    bool IsAdmin,
    bool IsApiKey,
    bool CanManage);

public enum ChannelContextFailure
{
  None,
  Missing,
  NotFound,
  Inaccessible
}

public sealed record ChannelContextResolution(
    ChannelContext? Context,
    ChannelContextFailure Failure)
{
  public bool Succeeded => Context is not null && Failure == ChannelContextFailure.None;
}

public interface IChannelContextResolver
{
  Task<ChannelContextResolution> ResolveAsync(HttpContext context);
  Task<IReadOnlyList<YTChannel>> GetAccessibleChannelsAsync(ClaimsPrincipal principal);
}

public sealed class ChannelContextResolver(
    IYTChannelRepository channelRepository,
    IUserChannelOwnerRepository ownerRepository,
    IUserAccessResolver userAccessResolver,
    IApiKeyRepository apiKeyRepository) : IChannelContextResolver
{
  public async Task<ChannelContextResolution> ResolveAsync(HttpContext context)
  {
    var requestedChannelId = context.Request.Headers[ChannelContextConstants.HeaderName]
        .FirstOrDefault()?.Trim();
    if (string.IsNullOrWhiteSpace(requestedChannelId))
    {
      return new(null, ChannelContextFailure.Missing);
    }

    var channel = (await channelRepository.GetItemsAsync(x => x.ChannelId == requestedChannelId)).FirstOrDefault();
    if (channel is null)
    {
      return new(null, ChannelContextFailure.NotFound);
    }

    var principal = context.User;
    var apiKeyId = principal.FindFirstValue("ApiKeyId");
    if (!string.IsNullOrWhiteSpace(apiKeyId))
    {
      var apiKey = await apiKeyRepository.GetItemAsync(apiKeyId);
      if (apiKey?.ChannelId != requestedChannelId)
      {
        return new(null, ChannelContextFailure.Inaccessible);
      }

      return new(new ChannelContext(requestedChannelId, channel, false, true, true), ChannelContextFailure.None);
    }

    var effectiveUserId = ImpersonationClaimsTransformation.GetEffectiveUserId(principal);
    var profile = await userAccessResolver.ResolveAsync(effectiveUserId);
    var isAdmin = profile?.GroupCodes.Contains(AuthorizationGroupCodes.Admin, StringComparer.OrdinalIgnoreCase) == true ||
        profile?.EffectivePermissions.Contains(AuthorizationPermissionKeys.BackofficeManageAll, StringComparer.OrdinalIgnoreCase) == true ||
        (!principal.HasClaim("impersonation", "true") && principal.FindAll("permission")
            .Select(claim => UserAccessResolver.Normalize(claim.Value))
            .Contains(AuthorizationPermissionKeys.BackofficeManageAll, StringComparer.OrdinalIgnoreCase));

    if (isAdmin)
    {
      return new(new ChannelContext(requestedChannelId, channel, true, false, true), ChannelContextFailure.None);
    }

    if (string.IsNullOrWhiteSpace(effectiveUserId) ||
        !(await ownerRepository.GetByUserIdAsync(effectiveUserId)).Any(owner =>
            owner.IsActive && owner.ChannelId == requestedChannelId))
    {
      return new(null, ChannelContextFailure.Inaccessible);
    }

    return new(new ChannelContext(requestedChannelId, channel, false, false, true), ChannelContextFailure.None);
  }

  public async Task<IReadOnlyList<YTChannel>> GetAccessibleChannelsAsync(ClaimsPrincipal principal)
  {
    var apiKeyId = principal.FindFirstValue("ApiKeyId");
    if (!string.IsNullOrWhiteSpace(apiKeyId))
    {
      var apiKey = await apiKeyRepository.GetItemAsync(apiKeyId);
      if (string.IsNullOrWhiteSpace(apiKey?.ChannelId))
      {
        return [];
      }

      return (await channelRepository.GetItemsAsync(x => x.ChannelId == apiKey.ChannelId)).ToArray();
    }

    var effectiveUserId = ImpersonationClaimsTransformation.GetEffectiveUserId(principal);
    var profile = await userAccessResolver.ResolveAsync(effectiveUserId);
    var isAdmin = profile?.GroupCodes.Contains(AuthorizationGroupCodes.Admin, StringComparer.OrdinalIgnoreCase) == true ||
        profile?.EffectivePermissions.Contains(AuthorizationPermissionKeys.BackofficeManageAll, StringComparer.OrdinalIgnoreCase) == true;

    if (isAdmin)
    {
      return (await channelRepository.GetItemsAsync()).OrderBy(channel => channel.ChannelName).ToArray();
    }

    var ownedIds = (await ownerRepository.GetByUserIdAsync(effectiveUserId))
        .Where(owner => owner.IsActive)
        .Select(owner => owner.ChannelId)
        .ToHashSet(StringComparer.Ordinal);
    return (await channelRepository.GetItemsAsync(channel => ownedIds.Contains(channel.ChannelId)))
        .OrderBy(channel => channel.ChannelName)
        .ToArray();
  }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class RequireChannelScopeAttribute : Attribute;

public sealed class ChannelScopeMiddleware(RequestDelegate next)
{
  public async Task InvokeAsync(HttpContext context, IChannelContextResolver resolver)
  {
    if (context.GetEndpoint()?.Metadata.GetMetadata<RequireChannelScopeAttribute>() is null)
    {
      await next(context);
      return;
    }

    var resolution = await resolver.ResolveAsync(context);
    if (!resolution.Succeeded)
    {
      var statusCode = resolution.Failure == ChannelContextFailure.Missing
          ? StatusCodes.Status400BadRequest
          : StatusCodes.Status404NotFound;
      context.Response.StatusCode = statusCode;
      await context.Response.WriteAsJsonAsync(new
      {
        code = resolution.Failure == ChannelContextFailure.Missing
              ? "channel_context_required"
              : "channel_context_unavailable",
        message = resolution.Failure == ChannelContextFailure.Missing
              ? $"{ChannelContextConstants.HeaderName} header is required"
              : "The selected channel was not found or is not accessible"
      });
      return;
    }

    context.Items[ChannelContextConstants.ItemKey] = resolution.Context!;
    await next(context);
  }
}

public static class ChannelContextHttpContextExtensions
{
  public static ChannelContext GetChannelContext(this HttpContext context) =>
      context.Items.TryGetValue(ChannelContextConstants.ItemKey, out var value) && value is ChannelContext channelContext
          ? channelContext
          : throw new InvalidOperationException("The endpoint does not have a resolved channel context.");
}