using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class BootstrapAdminHeaderOperationFilterTests
{
    [Fact]
    public void Adds_required_bootstrap_secret_header_only_to_bootstrap_admin_operation()
    {
        var filter = new BootstrapAdminHeaderOperationFilter();

        var bootstrapOperation = new OpenApiOperation();
        filter.Apply(bootstrapOperation, CreateContext(nameof(UserController.BootstrapAdmin)));

        var bootstrapHeader = Assert.Single(bootstrapOperation.Parameters!);
        Assert.Equal("X-Bootstrap-Secret", bootstrapHeader.Name);
        Assert.Equal(ParameterLocation.Header, bootstrapHeader.In);
        Assert.True(bootstrapHeader.Required);

        var createUserOperation = new OpenApiOperation();
        filter.Apply(createUserOperation, CreateContext(nameof(UserController.CreateUser)));

        Assert.Null(createUserOperation.Parameters);
    }

    [Fact]
    public void Does_not_duplicate_bootstrap_secret_header_when_already_defined()
    {
        var filter = new BootstrapAdminHeaderOperationFilter();

        var operation = new OpenApiOperation
        {
            Parameters =
            [
                new OpenApiParameter
                {
                    Name = "X-Bootstrap-Secret",
                    In = ParameterLocation.Header,
                    Required = false,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            ]
        };

        filter.Apply(operation, CreateContext(nameof(UserController.BootstrapAdmin)));

        Assert.Single(operation.Parameters);
    }

    [Fact]
    public void Security_requirements_remain_unset_for_allow_anonymous_bootstrap_admin()
    {
        var securityFilter = new SecurityRequirementsOperationFilter();
        var operation = new OpenApiOperation();

        securityFilter.Apply(operation, CreateContext(nameof(UserController.BootstrapAdmin)));

        Assert.Null(operation.Security);
    }

    private static OperationFilterContext CreateContext(string methodName)
    {
        var method = typeof(UserController).GetMethod(methodName);
        Assert.NotNull(method);

        return new OperationFilterContext(
            new ApiDescription(),
            null!,
            new SchemaRepository(),
            new OpenApiDocument(),
            method);
    }
}