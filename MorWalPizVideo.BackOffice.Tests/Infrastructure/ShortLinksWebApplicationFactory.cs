using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public sealed class ShortLinksWebApplicationFactory : WebApplicationFactory<MorWalPizVideo.ShortLinks.Program>
{
    static ShortLinksWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableMock", "true");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableDev", "true");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableSwagger", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableKeyVault", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableCache", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:EnableMock"] = "true",
                ["FeatureManagement:EnableDev"] = "true",
                ["FeatureManagement:EnableSwagger"] = "false",
                ["FeatureManagement:EnableKeyVault"] = "false",
                ["FeatureManagement:EnableCache"] = "false"
            });
        });
    }
}
