using System.Net;
using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Configuration;
using MorWalPizVideo.BackOffice.Jobs;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class HangfireConfigurationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public HangfireConfigurationTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Disabled_mode_has_no_dashboard_or_hangfire_health_probes()
  {
    using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      AllowAutoRedirect = false
    });

    using var dashboardResponse = await client.GetAsync("/hangfire");
    Assert.Equal(HttpStatusCode.NotFound, dashboardResponse.StatusCode);

    var healthOptions = _factory.Services
        .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
        .Value;
    Assert.DoesNotContain(healthOptions.Registrations, registration =>
        registration.Name.Contains("hangfire", StringComparison.OrdinalIgnoreCase));
    Assert.Null(_factory.Services.GetService<IBackgroundJobClient>());
    Assert.Null(_factory.Services.GetService<JobStorage>());
    Assert.DoesNotContain(
        _factory.Services.GetServices<IHostedService>(),
        service => service.GetType().Name.Contains("BackgroundJobServer", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void Enabled_mode_without_durable_connection_fails_fast()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["ConnectionStrings:HangfireConnection"] = ""
        })
        .Build();

    var exception = Assert.Throws<InvalidOperationException>(() =>
        HangfireConfiguration.GetRequiredConnectionString(configuration));

    Assert.Contains("durable", exception.Message, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("HangfireConnection", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public void Checked_in_defaults_keep_hangfire_disabled_and_preserve_recurring_schedules()
  {
    Assert.False(_factory.Services.GetRequiredService<IConfiguration>()
        .GetValue<bool>("FeatureManagement:EnableHangFire"));
    Assert.Equal("news-job", NewsJobs.JobId);
    Assert.Equal("0 18 * * 0", NewsJobs.CronSchedule);
    Assert.Equal("youtube-sync-job", YouTubeSyncJob.JobId);
    Assert.Equal("YouTubeSyncCron", YouTubeSyncJob.CronConfigurationKey);
    Assert.Equal("0 3 * * *", YouTubeSyncJob.DefaultCronSchedule);
  }

  [Theory]
  [InlineData(true, true)]
  [InlineData(true, false)]
  [InlineData(false, true)]
  public async Task Dashboard_requires_authenticated_admin_group(
      bool authenticated,
      bool hasAdminGroup)
  {
    var userId = Guid.NewGuid().ToString("N");
    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, userId)],
        authenticated ? "Test" : null);
    var principal = new ClaimsPrincipal(identity);

    var resolver = new TestUserAccessResolver(hasAdminGroup, userId);
    var result = await HangfireAdminAuthorizationFilter.IsAdminAsync(principal, resolver);

    Assert.Equal(authenticated && hasAdminGroup, result);
  }

  private sealed class TestUserAccessResolver(bool hasAdminGroup, string expectedUserId) : IUserAccessResolver
  {
    public Task<UserAccessProfile?> ResolveAsync(string userId)
    {
      if (!string.Equals(userId, expectedUserId, StringComparison.Ordinal))
      {
        return Task.FromResult<UserAccessProfile?>(null);
      }

      var groups = hasAdminGroup
          ? new HashSet<string>([AuthorizationGroupCodes.Admin], StringComparer.OrdinalIgnoreCase)
          : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      var profile = new UserAccessProfile(
          new User { Id = userId, IsActive = true },
          groups,
          new HashSet<string>(StringComparer.OrdinalIgnoreCase),
          new HashSet<string>(StringComparer.OrdinalIgnoreCase));

      return Task.FromResult<UserAccessProfile?>(profile);
    }
  }
}