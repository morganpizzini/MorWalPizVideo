using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AskEndpointTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public AskEndpointTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Ask_routes_enforce_authentication_permission_and_channel_scope()
    {
        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        using var denied = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);
        using var allowed = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskView);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/Ask")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/api/Ask")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await allowed.GetAsync("/api/Ask")).StatusCode);
    }

    [Fact]
    public async Task Ask_create_update_and_read_are_channel_scoped_and_invalidate_cache()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskManage);
        var slug = $"endpoint-{Guid.NewGuid():N}";
        var request = new { title = "Endpoint campaign", description = "Description", slug, status = AskCampaignStatus.Published };

        var create = await client.PostAsJsonAsync("/api/Ask", request);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        var update = await client.PutAsJsonAsync($"/api/Ask/{id}", new { title = "Updated", request.description, request.slug, request.status });
        using var otherChannel = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskManage);
        otherChannel.DefaultRequestHeaders.Remove("X-Channel-Id");
        otherChannel.DefaultRequestHeaders.Add("X-Channel-Id", "missing-channel");
        var hidden = await otherChannel.GetAsync($"/api/Ask/{id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Contains(CacheKeys.Ask, factory.CrossApiService.ResetKeys);
    }
}