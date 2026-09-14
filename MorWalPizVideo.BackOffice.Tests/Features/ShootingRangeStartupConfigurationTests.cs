using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

public sealed class ShootingRangeStartupConfigurationTests
{
    [Fact]
    public void DirectMongoConfigurationIsAcceptedWhenKeyVaultIsDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MorWalPizDatabase:ConnectionString"] = "mongodb://localhost",
                ["MorWalPizDatabase:DatabaseName"] = "shooting-range"
            })
            .Build();

        StartupConfigurationValidation.ValidateMongoConfiguration(configuration, null);
    }

    [Fact]
    public void KeyVaultConfigurationDoesNotFallBackToDirectMongoValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MorWalPizDatabase:ConnectionString"] = "mongodb://direct-fallback",
                ["MorWalPizDatabase:DatabaseName"] = "shooting-range"
            })
            .Build();
        var keyVaultProvider = new MemoryConfigurationProvider(new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["MorWalPizDatabase:ConnectionString"] = "mongodb://vault"
            }
        });

        var action = () => StartupConfigurationValidation.ValidateMongoConfiguration(configuration, keyVaultProvider);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*MorWalPizDatabase:DatabaseName*");
    }
}