using System.Security.Claims;
using Hangfire.Dashboard;

namespace MorWalPizVideo.BackOffice.Authentication;

public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
  public bool Authorize(DashboardContext context) => IsAdmin(context.GetHttpContext().User);

  public static bool IsAdmin(ClaimsPrincipal user) =>
      user.Identity?.IsAuthenticated == true &&
      user.FindAll(ClaimTypes.Role).Any(claim =>
          string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase));
}