using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

[Trait("Category", "TestGroup:ShootingRange")]
public sealed class ShootingRangeStartupConfigurationTests
{
    [Fact]
    public void DirectSqlServerConfigurationIsAcceptedWhenKeyVaultIsDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShootingRangeDatabase:ConnectionString"] = "test-connection"
            })
            .Build();

        StartupConfigurationValidation.ValidateSqlServerConfiguration(configuration, null);
    }

    [Fact]
    public void KeyVaultConfigurationDoesNotFallBackToDirectDatabaseValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShootingRangeDatabase:ConnectionString"] = "direct-fallback"
            })
            .Build();
        var keyVaultProvider = new MemoryConfigurationProvider(new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>()
        });

        var action = () => StartupConfigurationValidation.ValidateSqlServerConfiguration(configuration, keyVaultProvider);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*ShootingRangeDatabase:ConnectionString*");
    }
}