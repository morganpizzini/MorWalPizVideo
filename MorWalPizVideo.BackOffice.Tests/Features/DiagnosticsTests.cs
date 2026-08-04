using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class DiagnosticsTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory _factory;

    public DiagnosticsTests(BackOfficeWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Diagnostics_is_not_available_to_anonymous_users()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await client.GetAsync("/api/Diagnostics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_is_forbidden_to_contributors()
    {
        using var client = CreateClient("contributor");

        var response = await client.GetAsync("/api/Diagnostics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_returns_the_existing_shape_for_admins()
    {
        using var client = CreateClient("admin");

        var response = await client.GetAsync("/api/Diagnostics");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.TryGetProperty("status", out _));
        Assert.True(document.RootElement.TryGetProperty("checks", out _));
        Assert.True(document.RootElement.TryGetProperty("recentProblems", out var recentProblems));
        Assert.True(recentProblems.GetArrayLength() <= 25);
    }

    [Fact]
    public async Task Diagnostics_is_read_only()
    {
        using var client = CreateClient("admin");

        var response = await client.PostAsync("/api/Diagnostics", null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public void Problem_store_bounds_entries_and_redacts_sensitive_values()
    {
        var store = new DiagnosticsProblemStore();
        for (var index = 0; index < 110; index++)
        {
            store.Record(
                "backend",
                "Authorization: Bearer top-secret-token; password=secret-password; " +
                "Server=mongodb://user:secret-password@db.example/app",
                new Dictionary<string, string?>
                {
                    ["Authorization"] = "Bearer structured-token",
                    ["connectionString"] = "Server=db;Password=structured-password"
                });
        }

        var problems = store.GetRecent(100);
        var problem = problems[0];

        Assert.Equal(100, problems.Count);
        Assert.DoesNotContain("top-secret-token", problem.Message);
        Assert.DoesNotContain("secret-password", problem.Message);
        Assert.DoesNotContain("structured-token", problem.Properties["Authorization"]);
        Assert.DoesNotContain("structured-password", problem.Properties["connectionString"]);
        Assert.Contains("[REDACTED]", problem.Message);
    }

    private HttpClient CreateClient(string? role = null)
    {
        var client = _factory.CreateClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
        }

        return client;
    }
}