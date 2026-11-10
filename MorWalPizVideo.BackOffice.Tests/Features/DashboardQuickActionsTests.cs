using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class DashboardQuickActionsTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private const string Endpoint = "/api/v1/dashboard/quick-actions";
    private readonly BackOfficeWebApplicationFactory factory;

    public DashboardQuickActionsTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Defaults_are_permission_filtered_and_do_not_create_preferences()
    {
        using var client = factory.CreateClientWithPermissions("backoffice.access", "pages.create");
        var result = await Read(client);
        Assert.False(result.IsConfigured);
        Assert.Equal(["/blogposts/create"], result.VisibleActions.Select(item => item.Url));
        Assert.Null(await Repository.GetAsync("test-user-id", PrimaryScenario.ChannelId));
        using var response = await client.GetAsync(Endpoint);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Literal_custom_actions_round_trip_in_order_without_destination_validation()
    {
        using var client = CreateUser();
        QuickActionEntry[] entries =
        [
            new("one", "custom", Label: "  <strong>自由</strong>  ", Url: "  /missing?x=%2F&x=two#Fragment  "),
            new("two", "custom", Label: "", Url: ""),
            new("three", "custom", Label: "Other", Url: "not a valid URL"),
            new("four", "custom", Label: "External", Url: "https://example.test/Case?x=%2f"),
            new("five", "custom", Label: "Fragment", Url: "#local")
        ];
        var saved = await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest(entries));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var read = await Read(client);
        Assert.True(read.IsConfigured);
        Assert.Equal(entries, read.Actions);
        Assert.Equal(entries.Select(item => item.Url), read.VisibleActions.Select(item => item.Url));
        Assert.Equal(entries.Select(item => item.Label), read.VisibleActions.Select(item => item.Label));
    }

    [Fact]
    public async Task Explicit_empty_and_reset_are_different_and_repeated_reset_is_harmless()
    {
        using var client = CreateUser();
        (await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest([]))).EnsureSuccessStatusCode();
        var empty = await Read(client);
        Assert.True(empty.IsConfigured);
        Assert.Empty(empty.Actions);
        Assert.Empty(empty.VisibleActions);
        foreach (var _ in Enumerable.Range(0, 2))
        {
            (await client.DeleteAsync(Endpoint)).EnsureSuccessStatusCode();
            var defaults = await Read(client);
            Assert.False(defaults.IsConfigured);
            Assert.Equal(3, defaults.Actions.Count);
        }
    }

    [Fact]
    public async Task Preferences_are_isolated_by_both_user_and_channel()
    {
        using var first = CreateUser();
        using var second = CreateUser();
        var otherChannel = $"quick-actions-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(otherChannel, "Other"));
        (await first.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest([new("a", "custom", Label: "Private", Url: "/videos")]))).EnsureSuccessStatusCode();
        Assert.False((await Read(second)).IsConfigured);
        first.DefaultRequestHeaders.Remove("X-Channel-Id");
        first.DefaultRequestHeaders.Add("X-Channel-Id", otherChannel);
        Assert.False((await Read(first)).IsConfigured);
        (await first.DeleteAsync(Endpoint)).EnsureSuccessStatusCode();
        first.DefaultRequestHeaders.Remove("X-Channel-Id");
        first.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);
        Assert.Equal("Private", Assert.Single((await Read(first)).Actions).Label);
    }

    [Fact]
    public async Task Permission_revocation_hides_predefined_actions_without_pruning_saved_selection()
    {
        using var client = CreateUser();
        (await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest([
            new("video", "predefined", "videos.manage"),
            new("custom", "custom", Label: "Video custom", Url: "/videos")
        ]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Remove("X-Test-Permissions");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "backoffice.access");
        await AddOwner(client);
        var denied = await Read(client);
        Assert.Equal(2, denied.Actions.Count);
        Assert.Equal("custom", Assert.Single(denied.VisibleActions).Kind);
        (await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest(denied.Actions))).EnsureSuccessStatusCode();
        var forged = await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest([new("forged", "predefined", "videos.import")]));
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-Permissions");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "backoffice.manageall");
        Assert.Equal(2, (await Read(client)).VisibleActions.Count);
    }

    [Theory]
    [InlineData("""{"actions":[{"id":"a","kind":"custom","label":null,"url":"/videos"}]}""")]
    [InlineData("""{"actions":[{"id":"a","kind":"custom","label":"A","url":"/"},{"id":"a","kind":"custom","label":"B","url":"/"}]}""")]
    [InlineData("""{"actions":[{"id":"a","kind":"unknown"}]}""")]
    [InlineData("""{"actions":[{"id":"a","kind":"predefined","actionKey":"unknown"}]}""")]
    [InlineData("""{"actions":null}""")]
    public async Task Invalid_structures_fail_explicitly(string json)
    {
        using var client = CreateUser();
        using var response = await client.PutAsync(Endpoint, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Requests_above_the_total_size_limit_are_rejected()
    {
        using var client = CreateUser();
        var response = await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest([
            new("large", "custom", Label: new string('x', 64 * 1024), Url: "/")
        ]));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.False((await Read(client)).IsConfigured);
    }

    [Theory]
    [InlineData(65536, HttpStatusCode.OK)]
    [InlineData(65537, HttpStatusCode.RequestEntityTooLarge)]
    public async Task Request_size_boundary_is_exact(int size, HttpStatusCode expected)
    {
        using var client = CreateUser();
        const string prefix = "{\"actions\":[{\"id\":\"boundary\",\"kind\":\"custom\",\"label\":\"";
        const string suffix = "\",\"url\":\"/\"}]}";
        var json = prefix + new string('x', size - prefix.Length - suffix.Length) + suffix;
        using var response = await client.PutAsync(Endpoint,
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Retired_catalog_entries_are_retained_but_not_actionable()
    {
        using var client = CreateUser();
        var userId = client.DefaultRequestHeaders.GetValues("X-Test-UserId").Single();
        await Repository.UpsertAsync(new()
        {
            UserId = userId,
            ChannelId = PrimaryScenario.ChannelId,
            Actions = [new() { Id = "retired", Kind = "predefined", ActionKey = "retired.action" }]
        });
        var current = await Read(client);
        Assert.Single(current.Actions);
        Assert.Empty(current.VisibleActions);
        (await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest(current.Actions))).EnsureSuccessStatusCode();
        Assert.Single((await Read(client)).Actions);
    }

    [Fact]
    public async Task Authentication_and_channel_context_are_required()
    {
        using var client = CreateUser();
        client.DefaultRequestHeaders.Remove("X-Channel-Id");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Endpoint)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Channel-Id", "not-an-accessible-channel");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Endpoint)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Endpoint)).StatusCode);
        using var noAccess = factory.CreateClientWithPermissions("pages.view");
        noAccess.DefaultRequestHeaders.Add("X-Test-UserId", $"no-access-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Forbidden, (await noAccess.GetAsync(Endpoint)).StatusCode);
    }

    [Fact]
    public async Task Api_key_principals_cannot_access_personal_preferences()
    {
        using var client = CreateUser();
        var (key, plaintext) = await factory.Services.GetRequiredService<IApiKeyService>()
            .CreateApiKeyAsync($"quick-actions-{Guid.NewGuid():N}", "", PrimaryScenario.ChannelId);
        client.DefaultRequestHeaders.Add("X-Test-ApiKey", plaintext);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync(Endpoint, new SaveQuickActionsRequest([]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync(Endpoint)).StatusCode);
    }

    [Fact]
    public async Task Mock_repository_is_shared_across_scopes_atomic_and_resettable()
    {
        var scenario = new PrimaryScenario();
        var repository = new BackOfficeQuickActionPreferencesMockRepository(scenario);
        var otherInstance = new BackOfficeQuickActionPreferencesMockRepository(scenario);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(index => repository.UpsertAsync(new()
        {
            UserId = "user",
            ChannelId = "channel",
            Actions = [new() { Id = index.ToString(), Kind = "custom", Label = "", Url = "" }]
        })));
        Assert.Single(await repository.GetItemsAsync());
        Assert.Single((await otherInstance.GetAsync("user", "channel"))!.Actions);
        scenario.Reset();
        Assert.Null(await otherInstance.GetAsync("user", "channel"));
    }

    [Fact]
    public void Manifest_and_impersonation_mutation_metadata_are_explicit()
    {
        var index = MongoIndexOperationsService.Manifest.Single(item => item.Key == "quickactionpreferences.user_channel.unique");
        Assert.True(index.Unique);
        Assert.Equal(["userId", "channelId"], index.Keys.Names);
        var type = typeof(Controllers.DashboardQuickActionsController);
        foreach (var method in new[] { "Put", "Delete" })
            Assert.NotEmpty(type.GetMethod(method)!.GetCustomAttributes(typeof(Authentication.BlockImpersonationAttribute), true));
    }

    private IBackOfficeQuickActionPreferencesRepository Repository =>
        factory.Services.GetRequiredService<IBackOfficeQuickActionPreferencesRepository>();

    private HttpClient CreateUser()
    {
        var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeManageAll);
        client.DefaultRequestHeaders.Add("X-Test-UserId", $"qa-{Guid.NewGuid():N}");
        return client;
    }

    private async Task AddOwner(HttpClient client)
    {
        var repository = factory.Services.GetRequiredService<IUserChannelOwnerRepository>();
        await repository.AddItemAsync(new UserChannelOwner
        {
            UserId = client.DefaultRequestHeaders.GetValues("X-Test-UserId").Single(),
            ChannelId = PrimaryScenario.ChannelId,
            IsActive = true
        });
    }

    private static async Task<QuickActionsResponse> Read(HttpClient client)
    {
        var response = await client.GetAsync(Endpoint);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuickActionsResponse>())!;
    }
}
