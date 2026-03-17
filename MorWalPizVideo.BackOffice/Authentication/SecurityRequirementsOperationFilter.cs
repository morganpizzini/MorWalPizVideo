using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MorWalPizVideo.BackOffice.Authentication;

/// <summary>
/// Operation filter that adds security requirements to Swagger operations based on endpoint metadata
/// </summary>
public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if endpoint allows anonymous access
        var hasAllowAnonymous = context.MethodInfo.DeclaringType != null &&
            (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ||
             context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any());

        if (hasAllowAnonymous)
        {
            return; // No security requirement for anonymous endpoints
        }

        // Check for Authorize attribute
        var hasAuthorize = context.MethodInfo.DeclaringType != null &&
            (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
             context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any());

        // Check for ApiKeyAuth attribute
        var hasApiKeyAuth = context.MethodInfo.DeclaringType != null &&
            (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<ApiKeyAuthAttribute>().Any() ||
             context.MethodInfo.GetCustomAttributes(true).OfType<ApiKeyAuthAttribute>().Any());

        if (!hasAuthorize && !hasApiKeyAuth)
        {
            return; // No authorization required
        }

        operation.Security = new List<OpenApiSecurityRequirement>();

        if (hasApiKeyAuth)
        {
            // Endpoint requires API Key authentication
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
        else if (hasAuthorize)
        {
            // Endpoint requires JWT Bearer authentication
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}