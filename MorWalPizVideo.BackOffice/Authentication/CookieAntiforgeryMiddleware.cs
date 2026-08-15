using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace MorWalPizVideo.BackOffice.Authentication;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireCookieAntiforgeryAttribute : Attribute;

public sealed class CookieAntiforgeryMiddleware
{
  private const string ApiKeyScheme = "ApiKey";

  private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

  private readonly RequestDelegate _next;

  public CookieAntiforgeryMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
  {
    if (RequiresValidation(context))
    {
      try
      {
        await antiforgery.ValidateRequestAsync(context);
      }
      catch (AntiforgeryValidationException)
      {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { message = "Invalid CSRF token" });
        return;
      }
    }

    await _next(context);
  }

  private static bool RequiresValidation(HttpContext context)
  {
    if (SafeMethods.Contains(context.Request.Method) ||
        !context.Request.Cookies.ContainsKey("auth_token"))
    {
      return false;
    }

    var endpoint = context.GetEndpoint();
    if (endpoint is null)
    {
      return false;
    }

    if (endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
        .Any(metadata => HasAuthenticationScheme(metadata, ApiKeyScheme)))
    {
      return false;
    }

    return endpoint.Metadata.GetMetadata<RequireCookieAntiforgeryAttribute>() is not null ||
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;
  }

  private static bool HasAuthenticationScheme(IAuthorizeData metadata, string scheme)
  {
    return metadata.AuthenticationSchemes?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(authenticationScheme => string.Equals(authenticationScheme, scheme, StringComparison.OrdinalIgnoreCase)) == true;
  }
}