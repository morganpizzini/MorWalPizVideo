using System.Net;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class DashboardControllerTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public DashboardControllerTests(BackOfficeWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Video_publications_includes_days_without_videos()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);

        var response = await client.GetAsync("/api/dashboard/video-publications?days=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var days = document.RootElement.EnumerateArray().ToArray();

        Assert.Equal(3, days.Length);
        Assert.Equal(3, days.Select(day => day.GetProperty("date").GetDateTime().Date)
            .Distinct()
            .Count());
        Assert.Contains(days, day => day.GetProperty("count").GetInt32() == 0 &&
            day.GetProperty("videos").GetArrayLength() == 0);
    }
}
