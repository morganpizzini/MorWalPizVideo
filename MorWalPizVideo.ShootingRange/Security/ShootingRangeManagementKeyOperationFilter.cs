using Microsoft.OpenApi;
using MorWalPizVideo.ShootingRange.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeManagementKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ShootingRangeTenantManagementController))
            return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("ShootingRangeManagementKey", context.Document, null)] = []
            }
        ];
    }
}
