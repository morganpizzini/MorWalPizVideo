using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "__Host-shooting-range-csrf"; options.Cookie.HttpOnly = false; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.None; });
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.Cookie.Name = "shooting_range_auth"; options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.None; options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }; });
builder.Services.AddAuthorization();
var useMock = builder.Configuration.GetValue("FeatureManagement:EnableMock", builder.Environment.IsDevelopment());
var enableKeyVault = builder.Configuration.GetValue("FeatureManagement:EnableKeyVault", false);
IConfigurationProvider? keyVaultProvider = null;

if (enableKeyVault)
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
    if (string.IsNullOrWhiteSpace(keyVaultUrl))
    {
        StartupConfigurationValidation.HandleKeyVaultFailure("EnableKeyVault is true but KeyVaultUrl is not configured.", useMock, builder.Environment);
    }
    else if (!Uri.TryCreate(keyVaultUrl, UriKind.Absolute, out var keyVaultUri) || keyVaultUri.Scheme != Uri.UriSchemeHttps)
    {
        StartupConfigurationValidation.HandleKeyVaultFailure("EnableKeyVault is true but KeyVaultUrl is invalid.", useMock, builder.Environment);
    }
    else
    {
        try
        {
            var configurationRoot = (IConfigurationRoot)builder.Configuration;
            var providerCount = configurationRoot.Providers.Count();
            builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
            keyVaultProvider = configurationRoot.Providers.Skip(providerCount).Single();
        }
        catch (Exception error)
        {
            StartupConfigurationValidation.HandleKeyVaultFailure($"Azure Key Vault configuration could not be loaded from {keyVaultUri.Host}.", useMock, builder.Environment, error);
        }
    }
}

if (!useMock && !builder.Environment.IsDevelopment())
    StartupConfigurationValidation.ValidateMongoConfiguration(builder.Configuration, enableKeyVault ? keyVaultProvider : null);

if (useMock) builder.Services.AddSingleton<IShootingRangeRepositorySet, MockShootingRangeRepositorySet>();
else
{
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(builder.Configuration["MorWalPizDatabase:ConnectionString"] ?? throw new InvalidOperationException("MorWalPizDatabase:ConnectionString is required.")));
    builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["MorWalPizDatabase:DatabaseName"] ?? "morwalpizvideo"));
    builder.Services.AddSingleton<IShootingRangeRepositorySet, MongoShootingRangeRepositorySet>();
}
builder.Services.AddScoped<ShootingRangeService>();
var app = builder.Build();
app.MapDefaultEndpoints();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }

internal static class StartupConfigurationValidation
{
    internal static void HandleKeyVaultFailure(string message, bool useMock, IHostEnvironment environment, Exception? innerException = null)
    {
        if (!useMock && !environment.IsDevelopment())
            throw new InvalidOperationException(message, innerException);

        Console.WriteLine($"Warning: {message}");
    }

    internal static void ValidateMongoConfiguration(IConfiguration configuration, IConfigurationProvider? keyVaultProvider)
    {
        var requiredKeys = new[]
        {
            "MorWalPizDatabase:ConnectionString",
            "MorWalPizDatabase:DatabaseName"
        };

        var missingKeys = requiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]) || (keyVaultProvider is not null && (!keyVaultProvider.TryGet(key, out var value) || string.IsNullOrWhiteSpace(value))))
            .ToArray();

        if (missingKeys.Length > 0)
            throw new InvalidOperationException($"Required production configuration is missing: {string.Join(", ", missingKeys)}.");
    }
}
