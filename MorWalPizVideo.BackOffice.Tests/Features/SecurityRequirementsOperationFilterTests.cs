using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class SecurityRequirementsOperationFilterTests
{
    [Fact]
    public void Adds_api_key_security_requirement_to_api_key_operations()
    {
        var operation = Apply(nameof(ChatController.GetReviewDetails));

        var securityRequirement = Assert.Single(operation.Security!);
        var schemeReference = Assert.Single(securityRequirement.Keys);

        Assert.Equal("ApiKey", schemeReference.Reference.Id);
        Assert.Empty(securityRequirement[schemeReference]);

        using var serializedOperation = new StringWriter();
        var jsonWriter = new OpenApiJsonWriter(serializedOperation);
        operation.SerializeAsV3(jsonWriter);
        serializedOperation.Flush();

        using var document = JsonDocument.Parse(serializedOperation.ToString());
        var serializedRequirement = document.RootElement.GetProperty("security")[0];

        Assert.True(
            serializedRequirement.TryGetProperty("ApiKey", out var scopes),
            serializedOperation.ToString());
        Assert.Equal(JsonValueKind.Array, scopes.ValueKind);
        Assert.Empty(scopes.EnumerateArray());
    }

    [Fact]
    public void Adds_bearer_security_requirement_to_jwt_operations()
    {
        var operation = Apply(nameof(UserController.GetUsers));

        var securityRequirement = Assert.Single(operation.Security!);
        var schemeReference = Assert.Single(securityRequirement.Keys);

        Assert.Equal("Bearer", schemeReference.Reference.Id);
        Assert.Empty(securityRequirement[schemeReference]);
    }

    [Fact]
    public void Leaves_anonymous_operations_without_security_requirement()
    {
        var operation = Apply(nameof(UserController.BootstrapAdmin));

        Assert.Null(operation.Security);
    }

    private static OpenApiOperation Apply(string methodName)
    {
        var filter = new SecurityRequirementsOperationFilter();
        var operation = new OpenApiOperation();
        var method = FindMethod(methodName);

        filter.Apply(
            operation,
            new OperationFilterContext(
                new ApiDescription(),
                null!,
                new SchemaRepository(),
                CreateDocument(),
                method));

        return operation;
    }

    private static MethodInfo FindMethod(string methodName)
    {
        var method = typeof(ChatController).GetMethod(methodName)
            ?? typeof(UserController).GetMethod(methodName);

        Assert.NotNull(method);
        return method;
    }

    private static OpenApiDocument CreateDocument() => new()
    {
        Components = new OpenApiComponents
        {
            SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["ApiKey"] = new OpenApiSecurityScheme
                {
                    Name = "X-API-Key",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header
                },
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                }
            }
        }
    };
}