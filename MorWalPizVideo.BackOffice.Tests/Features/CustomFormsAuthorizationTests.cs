using System.Net;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class CustomFormsAuthorizationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory _factory;

    public CustomFormsAuthorizationTests(BackOfficeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Forms_manage_passes_response_view_authorization()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.FormsManage);

        var response = await client.GetAsync("/api/CustomForms/missing-form/responses");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}