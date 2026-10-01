using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.ServerAPI.Controllers;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using System.Net;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class HostSecurityContainmentTests
{
    [Theory]
    [InlineData("Development", true, "FakeScheme", true)]
    [InlineData("Development", false, "Bearer", false)]
    [InlineData("Test", true, "Bearer", false)]
    [InlineData("Production", true, "Bearer", false)]
    public async Task Host_environment_matrix_contains_fake_auth_and_exception_details(string environment, bool enabled, string scheme, bool detailed)
    {
        await using var parent = new ServerApiWebApplicationFactory();
        await using var factory = parent.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("FeatureManagement:EnableMock", (environment != "Production").ToString());
            builder.UseSetting("FeatureManagement:EnableDev", enabled.ToString());
            builder.UseSetting("FeatureManagement:EnableKeyVault", "false");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMongoDbService>();
                services.AddControllers().AddApplicationPart(typeof(HostSecurityProbeController).Assembly);
            });
        });
        (await factory.Services.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync())!.Name.Should().Be(scheme);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var error = await client.GetAsync("/__host-security-probe/error");
        error.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await error.Content.ReadAsStringAsync()).Contains("security-diagnostics-sentinel", StringComparison.Ordinal).Should().Be(detailed);
        using var protectedResponse = await client.GetAsync("/__host-security-probe/protected");
        protectedResponse.StatusCode.Should().Be(detailed ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void Every_current_controller_action_has_explicit_public_or_authenticated_metadata()
    {
        using var factory = new ServerApiWebApplicationFactory();
        var actions = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items;
        actions.Should().NotBeEmpty();
        actions.Should().OnlyContain(action => action.EndpointMetadata.OfType<IAllowAnonymous>().Any() || action.EndpointMetadata.OfType<IAuthorizeData>().Any(), "the host fallback must not silently change an inventoried route");
        factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value.FallbackPolicy.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Development", true, true)]
    [InlineData("Development", false, false)]
    [InlineData("Production", true, false)]
    [InlineData("Production", false, false)]
    [InlineData("Staging", true, false)]
    public async Task Diagnostics_require_actual_development_and_flag(string environment, bool enabled, bool allowed)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FeatureManagement:EnableDev"] = enabled.ToString() }).Build();
        using var provider = new ServiceCollection().AddFeatureManagement(configuration).Services.BuildServiceProvider();
        var controller = new ConfigTestController(provider.GetRequiredService<IFeatureManager>(), Options.Create(new MorWalPizDatabaseSettings()), new EnvironmentStub(environment));
        (await controller.TestConfiguration() is OkObjectResult).Should().Be(allowed);
        (await controller.TestDatabaseConnection() is OkObjectResult).Should().Be(allowed);
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    public void Missing_external_credentials_fail_closed_outside_development(string environment, bool rejected)
    {
        var configuration = new ConfigurationBuilder().Build();
        Action resolve = () => YouTubeCredentialProvisioning.ResolvePath(configuration, new EnvironmentStub(environment));
        if (rejected) resolve.Should().Throw<InvalidOperationException>();
        else YouTubeCredentialProvisioning.ResolvePath(configuration, new EnvironmentStub(environment)).Should().Be("credentials.json");
    }

    [Fact]
    public void External_provider_accepts_absolute_mount_but_rejects_relative_and_output_paths()
    {
        foreach (var path in new[] { "credentials.json", Path.Combine(AppContext.BaseDirectory, "credentials-sentinel.json") })
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["YouTube:CredentialsPath"] = path }).Build();
            Action resolve = () => YouTubeCredentialProvisioning.ResolvePath(configuration);
            resolve.Should().Throw<InvalidOperationException>();
        }
        var externalPath = Path.Combine(Path.GetTempPath(), "protected-youtube", "credentials-sentinel.json");
        var externalConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["YouTube:CredentialsPath"] = externalPath }).Build();
        YouTubeCredentialProvisioning.ResolvePath(externalConfiguration, new EnvironmentStub("Production")).Should().Be(externalPath);
    }

    private sealed class EnvironmentStub(string name) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "SecurityTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

[ApiController, Route("__host-security-probe")]
public sealed class HostSecurityProbeController : ControllerBase
{
    [HttpGet("error"), AllowAnonymous]
    public IActionResult Error() => throw new InvalidOperationException("security-diagnostics-sentinel");

    [HttpGet("protected")]
    public IActionResult Protected() => Ok();
}