using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using MorWalPizVideo.BackOffice.Services;
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

        if (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<RequireChannelScopeAttribute>().Any() == true ||
            context.MethodInfo.GetCustomAttributes(true).OfType<RequireChannelScopeAttribute>().Any())
        {
            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = ChannelContextConstants.HeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description = "External YouTube channel identifier used to scope this BackOffice operation.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }

        operation.Security = new List<OpenApiSecurityRequirement>();

        if (hasApiKeyAuth)
        {
            // Endpoint requires API Key authentication
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("ApiKey", null!, null),
                    new List<string>()
                }
            });
        }
        else if (hasAuthorize)
        {
            // Endpoint requires JWT Bearer authentication
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", null!, null),
                    new List<string>()
                }
            });
        }
    }
}