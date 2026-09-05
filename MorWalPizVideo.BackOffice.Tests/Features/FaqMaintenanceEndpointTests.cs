using System.Net;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class FaqMaintenanceEndpointTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public FaqMaintenanceEndpointTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Vote_reconciliation_requires_backoffice_manage_all()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.FaqManage);

        var response = await client.PostAsJsonAsync("/api/faq-maintenance/votes/reconcile", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Vote_reconciliation_is_available_to_backoffice_manage_all()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeManageAll);

        var response = await client.PostAsJsonAsync("/api/faq-maintenance/votes/reconcile", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}