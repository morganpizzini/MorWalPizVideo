using Microsoft.OpenApi;
using MorWalPizVideo.BackOffice.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MorWalPizVideo.BackOffice.Authentication;

/// <summary>
/// Adds the bootstrap secret header parameter only to the BootstrapAdmin operation.
/// </summary>
public sealed class BootstrapAdminHeaderOperationFilter : IOperationFilter
{
    private const string BootstrapSecretHeaderName = "X-Bootstrap-Secret";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!IsBootstrapAdminOperation(context))
        {
            return;
        }

        operation.Parameters ??= new List<IOpenApiParameter>();

        var alreadyDefined = operation.Parameters.Any(parameter =>
            parameter.In == ParameterLocation.Header &&
            string.Equals(parameter.Name, BootstrapSecretHeaderName, StringComparison.OrdinalIgnoreCase));

        if (alreadyDefined)
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = BootstrapSecretHeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Secret required to bootstrap the initial admin account.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }

    private static bool IsBootstrapAdminOperation(OperationFilterContext context)
    {
        return context.MethodInfo.DeclaringType == typeof(UserController) &&
               string.Equals(context.MethodInfo.Name, nameof(UserController.BootstrapAdmin), StringComparison.Ordinal);
    }
}