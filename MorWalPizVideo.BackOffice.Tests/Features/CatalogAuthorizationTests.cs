using System.Net;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class CatalogAuthorizationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory _factory;

    public CatalogAuthorizationTests(BackOfficeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(AuthorizationPermissionKeys.ProductsView, HttpStatusCode.OK)]
    [InlineData(AuthorizationPermissionKeys.ProductsManage, HttpStatusCode.OK)]
    [InlineData(AuthorizationPermissionKeys.BackofficeManageAll, HttpStatusCode.OK)]
    [InlineData(AuthorizationPermissionKeys.ProductsCreate, HttpStatusCode.Forbidden)]
    public async Task Product_catalog_read_enforces_exact_permission(
        string permission,
        HttpStatusCode expectedStatus)
    {
        using var client = CreateClient(permission);

        var response = await client.GetAsync("/api/Products");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthorizationPermissionKeys.ProductsCreate, HttpStatusCode.BadRequest)]
    [InlineData(AuthorizationPermissionKeys.ProductsManage, HttpStatusCode.BadRequest)]
    [InlineData(AuthorizationPermissionKeys.BackofficeManageAll, HttpStatusCode.BadRequest)]
    [InlineData(AuthorizationPermissionKeys.ProductsView, HttpStatusCode.Forbidden)]
    public async Task Product_catalog_create_enforces_exact_permission(
        string permission,
        HttpStatusCode expectedStatus)
    {
        using var client = CreateClient(permission);

        var response = await client.PostAsJsonAsync("/api/Products", new { });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthorizationPermissionKeys.ProductCategoriesView, HttpStatusCode.OK)]
    [InlineData(AuthorizationPermissionKeys.ProductCategoriesManage, HttpStatusCode.OK)]
    [InlineData(AuthorizationPermissionKeys.BackofficeManageAll, HttpStatusCode.OK)]
    [InlineData(AuthorizationPermissionKeys.ProductCategoriesCreate, HttpStatusCode.Forbidden)]
    public async Task Product_category_catalog_read_enforces_exact_permission(
        string permission,
        HttpStatusCode expectedStatus)
    {
        using var client = CreateClient(permission);

        var response = await client.GetAsync("/api/ProductCategories");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthorizationPermissionKeys.ProductCategoriesCreate, HttpStatusCode.BadRequest)]
    [InlineData(AuthorizationPermissionKeys.ProductCategoriesManage, HttpStatusCode.BadRequest)]
    [InlineData(AuthorizationPermissionKeys.BackofficeManageAll, HttpStatusCode.BadRequest)]
    [InlineData(AuthorizationPermissionKeys.ProductCategoriesView, HttpStatusCode.Forbidden)]
    public async Task Product_category_catalog_create_enforces_exact_permission(
        string permission,
        HttpStatusCode expectedStatus)
    {
        using var client = CreateClient(permission);

        var response = await client.PostAsJsonAsync("/api/ProductCategories", new { });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private HttpClient CreateClient(string permission)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
        return client;
    }
}