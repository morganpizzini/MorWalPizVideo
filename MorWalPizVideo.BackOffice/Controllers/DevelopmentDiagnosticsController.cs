using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Utils;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/development-diagnostics")]
[AllowAnonymous]
public sealed class DevelopmentDiagnosticsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DevelopmentDiagnosticsController(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("configuration")]
    public IActionResult GetConfiguration()
    {
        if (!_environment.IsDevelopment() || !_configuration.IsFeatureEnabled(MyFeatureFlags.EnableDev))
        {
            return NotFound();
        }

        var database = _configuration.GetSection("MorWalPizDatabase");
        var jwt = _configuration.GetSection("JwtSettings");
        var azureOpenAi = _configuration.GetSection("AzureConfig:OpenAi");
        var keyVaultUrl = _configuration["KeyVaultUrl"];

        return Ok(new
        {
            environment = _environment.EnvironmentName,
            featureFlags = new
            {
                enableDev = true,
                enableMock = _configuration.IsFeatureEnabled(MyFeatureFlags.EnableMock),
                enableKeyVault = _configuration.IsFeatureEnabled(MyFeatureFlags.EnableKeyVault),
                enableSwagger = _configuration.IsFeatureEnabled(MyFeatureFlags.EnableSwagger)
            },
            keyVault = new
            {
                enabled = _configuration.IsFeatureEnabled(MyFeatureFlags.EnableKeyVault),
                urlConfigured = HasValue(keyVaultUrl),
                host = Uri.TryCreate(keyVaultUrl, UriKind.Absolute, out var keyVaultUri)
                    ? keyVaultUri.Host
                    : null
            },
            mongo = new
            {
                connectionStringConfigured = HasValue(database["ConnectionString"]),
                databaseNameConfigured = HasValue(database["DatabaseName"])
            },
            jwt = new
            {
                secretConfigured = HasValue(jwt["Secret"]),
                secretLength = jwt["Secret"]?.Length ?? 0,
                issuerConfigured = HasValue(jwt["Issuer"]),
                audienceConfigured = HasValue(jwt["Audience"]),
                expirationDays = jwt["ExpirationDays"],
                expirationHours = jwt["ExpiryHours"]
            },
            azureOpenAi = new
            {
                endpointConfigured = HasValue(azureOpenAi["OpenAiEndpoint"]),
                deploymentConfigured = HasValue(azureOpenAi["DeploymentName"]),
                keyConfigured = HasValue(azureOpenAi["OpenAiKey"])
            },
            siteUrlConfigured = HasValue(_configuration["SiteUrl"])
        });
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);
}