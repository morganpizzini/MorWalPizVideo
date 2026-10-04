using System.Net;
using System.Net.Http.Json;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Domain.Scenarios;

namespace MorWalPizVideo.BackOffice.Tests.Features;

/// <summary>
/// Anonymous per-channel push subscription surface on ServerAPI. These endpoints must stay reachable without any
/// identity and must never echo the raw push endpoint back to the caller.
/// </summary>
[Trait("Category", "TestGroup:ServerAPI")]
public sealed class PushSubscriptionEndpointTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;

    public PushSubscriptionEndpointTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Subscribe_mints_a_credential_once_and_never_returns_the_endpoint()
    {
        using var client = factory.CreateClient();
        var endpoint = NewEndpoint();

        var response = await client.PostAsJsonAsync("/api/push/subscriptions", NewSubscribeRequest(endpoint, [PrimaryScenario.ChannelId]));
        var payload = await response.Content.ReadFromJsonAsync<PushSubscriptionStateContract>();
        var rawBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Credential));
        Assert.Equal(64, payload.Credential!.Length);
        Assert.Equal([PrimaryScenario.ChannelId], payload.ChannelIds);
        Assert.DoesNotContain(endpoint, rawBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Subscribe_requires_the_credential_to_retarget_an_existing_endpoint()
    {
        using var client = factory.CreateClient();
        var endpoint = NewEndpoint();
        var created = await (await client.PostAsJsonAsync("/api/push/subscriptions", NewSubscribeRequest(endpoint, [PrimaryScenario.ChannelId])))
            .Content.ReadFromJsonAsync<PushSubscriptionStateContract>();

        var withoutCredential = await client.PostAsJsonAsync("/api/push/subscriptions", NewSubscribeRequest(endpoint, ["other-channel"]));
        var withCredential = await client.PostAsJsonAsync("/api/push/subscriptions",
            NewSubscribeRequest(endpoint, ["other-channel"], created!.Credential));
        var updated = await withCredential.Content.ReadFromJsonAsync<PushSubscriptionStateContract>();

        Assert.Equal(HttpStatusCode.Forbidden, withoutCredential.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withCredential.StatusCode);
        Assert.Equal(["other-channel"], updated!.ChannelIds);
        Assert.Null(updated.Credential);
    }

    [Fact]
    public async Task Settings_and_revoke_require_a_matching_credential()
    {
        using var client = factory.CreateClient();
        var endpoint = NewEndpoint();
        var created = await (await client.PostAsJsonAsync("/api/push/subscriptions", NewSubscribeRequest(endpoint, [PrimaryScenario.ChannelId])))
            .Content.ReadFromJsonAsync<PushSubscriptionStateContract>();

        var wrongCredential = await client.PostAsJsonAsync("/api/push/subscriptions/settings",
            new { endpoint, credential = PushEndpointProtection.CreateCredential() });
        var settings = await client.PostAsJsonAsync("/api/push/subscriptions/settings",
            new { endpoint, credential = created!.Credential });
        var settingsPayload = await settings.Content.ReadFromJsonAsync<PushSubscriptionStateContract>();
        var revoke = await client.PostAsJsonAsync("/api/push/subscriptions/revoke",
            new { endpoint, credential = created.Credential });
        var afterRevoke = await client.PostAsJsonAsync("/api/push/subscriptions/settings",
            new { endpoint, credential = created.Credential });
        var afterRevokePayload = await afterRevoke.Content.ReadFromJsonAsync<PushSubscriptionStateContract>();

        Assert.Equal(HttpStatusCode.NotFound, wrongCredential.StatusCode);
        Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
        Assert.Equal([PrimaryScenario.ChannelId], settingsPayload!.ChannelIds);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Empty(afterRevokePayload!.ChannelIds);
    }

    [Fact]
    public async Task Subscribe_rejects_payloads_without_a_secure_endpoint_or_channel()
    {
        using var client = factory.CreateClient();

        var noChannels = await client.PostAsJsonAsync("/api/push/subscriptions", NewSubscribeRequest(NewEndpoint(), []));
        var insecure = await client.PostAsJsonAsync("/api/push/subscriptions", new
        {
            endpoint = "http://insecure.example/push/abc",
            keys = new { p256dh = "key", auth = "auth" },
            channelIds = new[] { PrimaryScenario.ChannelId },
            applicationKey = "morwalpizvideo"
        });

        Assert.Equal(HttpStatusCode.BadRequest, noChannels.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, insecure.StatusCode);
    }

    [Fact]
    public async Task Public_key_endpoint_is_anonymous_and_reports_unconfigured_state()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/push/subscriptions/public-key");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static string NewEndpoint() => $"https://push.example.com/send/{Guid.NewGuid():N}";

    private static object NewSubscribeRequest(string endpoint, string[] channelIds, string? credential = null) => new
    {
        endpoint,
        keys = new { p256dh = "BPublicKeyMaterial", auth = "AuthSecret" },
        channelIds,
        applicationKey = "morwalpizvideo",
        language = "IT",
        credential
    };
}
