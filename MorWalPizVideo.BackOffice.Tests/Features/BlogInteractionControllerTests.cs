using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class BlogInteractionControllerTests
{
    [Fact]
    public async Task Account_directory_is_global_but_access_controls_are_channel_scoped()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        const string accountId = "800000000000000000000002";
        var accounts = factory.Services.GetRequiredService<IPublicAccountRepository>();
        await accounts.AddAccountAsync(new PublicAccount
        {
            Id = accountId,
            Username = "channel-reader",
            NormalizedUsername = "CHANNEL-READER",
            Email = "channel-reader@example.test",
            NormalizedEmail = "CHANNEL-READER@EXAMPLE.TEST",
            DisplayName = "Channel Reader",
            IsActive = true
        });

        using var deniedClient = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);
        Assert.Equal(HttpStatusCode.Forbidden, (await deniedClient.GetAsync("/api/publicaccounts")).StatusCode);

        using var directoryClient = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PublicAccountsView);
        var directoryResponse = await directoryClient.GetAsync("/api/publicaccounts?search=channel-reader");
        Assert.Equal(HttpStatusCode.OK, directoryResponse.StatusCode);
        var directoryJson = await JsonDocument.ParseAsync(await directoryResponse.Content.ReadAsStreamAsync());
        Assert.Equal(1, directoryJson.RootElement.GetProperty("totalCount").GetInt32());
        Assert.False(directoryJson.RootElement.GetProperty("items")[0].TryGetProperty("email", out _));

        using var interactionClient = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BlogInteractionsView);
        var detailResponse = await interactionClient.GetAsync($"/api/blog-interactions/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailJson = await JsonDocument.ParseAsync(await detailResponse.Content.ReadAsStreamAsync());
        Assert.False(detailJson.RootElement.GetProperty("account").TryGetProperty("email", out _));

        using var accessClient = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PortalAccessManage);
        var updateResponse = await accessClient.PutAsJsonAsync($"/api/blog-interactions/accounts/{accountId}/access",
            new { revision = 0, commentsBlocked = true, portalAccessRevoked = false, reason = "Spam" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedResponse = await interactionClient.GetAsync($"/api/blog-interactions/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updatedJson = await JsonDocument.ParseAsync(await updatedResponse.Content.ReadAsStreamAsync());
        var access = updatedJson.RootElement.GetProperty("account").GetProperty("portalAccess");
        Assert.True(access.GetProperty("commentsBlocked").GetBoolean());
        Assert.False(access.GetProperty("portalAccessRevoked").GetBoolean());
        Assert.Equal(PrimaryScenario.ChannelId, access.GetProperty("channelId").GetString());
    }
}
