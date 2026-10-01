using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class CalendarControllerTests(PageControllerWebApplicationFactory factory) : IClassFixture<PageControllerWebApplicationFactory>
{
    private static SaveCalendarEventRequest Request() => new()
    {
        Title = $"Calendar-{Guid.NewGuid():N}", Description = "Description",
        StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 2),
        Categories = [new CategoryRef("category-id", "Category title")], MatchId = "match-id",
        ChannelId = "untrusted-channel"
    };

    [Fact]
    public async Task Create_read_rename_and_delete_preserve_identity_and_server_channel()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.CalendarManage);
        var request = Request();
        var created = await client.PostAsJsonAsync("/api/calendarEvents", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var post = (await created.Content.ReadFromJsonAsync<CalendarEventContract>())!;
        Assert.Equal(PrimaryScenario.ChannelId, post.ChannelId);
        Assert.Equal(request.Categories, post.Categories);
        Assert.Equal(request.MatchId, post.MatchId);
        var original = await factory.Services.GetRequiredService<ICalendarService>().GetAsync(post.Id, post.ChannelId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/calendarEvents/by-title/{post.Title}")).StatusCode);
        var renamed = await client.PutAsJsonAsync($"/api/calendarEvents/{post.Id}", request with
        { Id = post.Id, Title = request.Title + " renamed", CreationDateTime = DateTime.MinValue });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var current = (await renamed.Content.ReadFromJsonAsync<CalendarEventContract>())!;
        Assert.Equal(post.Id, current.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/calendarEvents/{post.Id}")).StatusCode);
        Assert.Equal(post.ChannelId, current.ChannelId);
        Assert.Equal(original!.CreationDateTime, (await factory.Services.GetRequiredService<ICalendarService>().GetAsync(post.Id))!.CreationDateTime);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/calendarEvents/{post.Id}")).StatusCode);
    }

    [Fact]
    public async Task Duplicate_create_is_global_case_insensitive_but_duplicate_update_remains_allowed()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.CalendarManage);
        var request = Request();
        var created = await client.PostAsJsonAsync("/api/calendarEvents", request);
        var first = (await created.Content.ReadFromJsonAsync<CalendarEventContract>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/calendarEvents", request with { Title = request.Title.ToUpperInvariant() })).StatusCode);
        var secondResponse = await client.PostAsJsonAsync("/api/calendarEvents", Request());
        var second = (await secondResponse.Content.ReadFromJsonAsync<CalendarEventContract>())!;
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/calendarEvents/{second.Id}", request with { Id = second.Id })).StatusCode);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Validation_mismatched_identity_and_permissions_are_http_contracts()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.CalendarManage);
        var request = Request();
        foreach (var invalid in new[] { request with { Title = " " }, request with { Description = "" },
            request with { StartDate = default }, request with { EndDate = request.StartDate.AddDays(-1) },
            request with { Categories = null! }, request with { Categories = [new CategoryRef("", "Invalid")] } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/calendarEvents", invalid)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/calendarEvents/route-id", request with { Id = "body-id" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/calendarEvents/missing", request)).StatusCode);
        using var denied = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.PostAsJsonAsync("/api/calendarEvents", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/api/calendarEvents")).StatusCode);
    }

    [Fact]
    public async Task Revision_aware_stale_update_delete_and_legacy_http_mutations_are_compatible()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.CalendarManage);
        var request = Request();
        var post = (await (await client.PostAsJsonAsync("/api/calendarEvents", request)).Content.ReadFromJsonAsync<CalendarEventContract>())!;
        Assert.Equal(1, post.Revision);
        var saved = await client.PutAsJsonAsync($"/api/calendarEvents/{post.Id}", request with { Id = post.Id, Revision = 1 });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(2, (await saved.Content.ReadFromJsonAsync<CalendarEventContract>())!.Revision);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/calendarEvents/{post.Id}", request with { Id = post.Id, Revision = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/calendarEvents/{post.Id}?revision=1")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/calendarEvents/{post.Id}?revision=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/calendarEvents/{post.Id}", request with { Revision = -1 })).StatusCode);
        var blind = await client.PutAsJsonAsync($"/api/calendarEvents/{post.Id}", request with { Id = post.Id });
        Assert.Equal(3, (await blind.Content.ReadFromJsonAsync<CalendarEventContract>())!.Revision);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/calendarEvents/{post.Id}?revision=2")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/calendarEvents/{post.Id}?revision=3")).StatusCode);
    }

    [Fact]
    public async Task Other_channel_cannot_read_update_or_delete_and_body_channel_cannot_override_scope()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.CalendarManage);
        var request = Request();
        var post = (await (await client.PostAsJsonAsync("/api/calendarEvents", request)).Content.ReadFromJsonAsync<CalendarEventContract>())!;
        var otherChannel = $"channel-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(otherChannel, "Other"));
        using var other = factory.CreateClient();
        other.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.BackofficeManageAll);
        other.DefaultRequestHeaders.Add("X-Channel-Id", otherChannel);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/calendarEvents/by-title/{post.Title}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/calendarEvents/{post.Id}")).StatusCode);
        Assert.DoesNotContain((await other.GetFromJsonAsync<CalendarEventContract[]>("/api/calendarEvents"))!, item => item.Id == post.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/calendarEvents/{post.Id}", request with { Id = post.Id, ChannelId = post.ChannelId })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/calendarEvents/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await other.PostAsJsonAsync("/api/calendarEvents", request)).StatusCode);
    }
}