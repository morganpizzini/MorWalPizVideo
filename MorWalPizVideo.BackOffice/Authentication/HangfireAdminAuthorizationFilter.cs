using Hangfire.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using System.Security.Claims;

namespace MorWalPizVideo.BackOffice.Authentication;

public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
  public bool Authorize(DashboardContext context)
  {
    var httpContext = context.GetHttpContext();
    return IsAdminAsync(httpContext.User, httpContext.RequestServices.GetRequiredService<IUserAccessResolver>())
      .GetAwaiter()
      .GetResult();
  }

  public static async Task<bool> IsAdminAsync(ClaimsPrincipal user, IUserAccessResolver accessResolver)
  {
    if (user.Identity?.IsAuthenticated != true)
    {
      return false;
    }

    var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(user);
    if (string.IsNullOrWhiteSpace(userId))
    {
      return false;
    }

    var profile = await accessResolver.ResolveAsync(userId);
    return profile?.GroupCodes.Contains(AuthorizationGroupCodes.Admin, StringComparer.OrdinalIgnoreCase) == true;
  }
}