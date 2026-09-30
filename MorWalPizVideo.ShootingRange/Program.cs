using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Threading.RateLimiting;
using System.Security.Claims;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "__Host-shooting-range-csrf"; options.Cookie.HttpOnly = false; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.None; });
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("https://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddScoped<ShootingRangeSessions>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "shooting_range_auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.ExpireTimeSpan = ShootingRangeSessions.Lifetime;
    options.SlidingExpiration = false;
    options.Events.OnValidatePrincipal = context => context.HttpContext.RequestServices.GetRequiredService<ShootingRangeSessions>().ValidateAsync(context);
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("range-login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0, AutoReplenishment = true
    }));
});
builder.Services.AddAuthorization();
var useMock = builder.Configuration.GetValue("FeatureManagement:EnableMock", builder.Environment.IsDevelopment());
StartupConfigurationValidation.ValidateMockMode(useMock, builder.Environment);
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
    builder.Services.AddHealthChecks().AddCheck<ShootingRangeMongoReadiness>("range-mongo", tags: ["ready", "startup"]);
}
builder.Services.AddScoped<ShootingRangeService>();
var app = builder.Build();
if (!useMock)
{
    await ((MongoShootingRangeRepositorySet)app.Services.GetRequiredService<IShootingRangeRepositorySet>()).VerifySecurityIndexesAsync();
    await ((MongoShootingRangeRepositorySet)app.Services.GetRequiredService<IShootingRangeRepositorySet>()).VerifyTransactionsAsync();
    app.Logger.LogInformation("Shooting range transaction guard preflight succeeded.");
}
app.MapDefaultEndpoints();
app.UseCors();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (Exception error) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested && error is ArgumentException or KeyNotFoundException or UnauthorizedAccessException or InvalidOperationException or MongoException or OperationCanceledException)
    {
        var status = error switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            InvalidOperationException => StatusCodes.Status409Conflict,
            MongoWriteException mongo when mongo.WriteError.Category == ServerErrorCategory.DuplicateKey => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status503ServiceUnavailable
        };
        app.Logger.LogWarning("Shooting range request rejected with {StatusCode} ({ErrorType}).", status, error.GetType().Name);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message = status == 503 ? "Transactional storage is unavailable. Refresh the current state before retrying." : error is MongoException ? "The selected slot is already pending or booked." : error.Message }, context.RequestAborted);
    }
});
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true && context.User.FindFirstValue(ShootingRangeSessions.ForcedChangeClaim) == "true" &&
        context.Request.Path.Value?.ToLowerInvariant() is not "/api/shooting-range/auth/session" and not "/api/shooting-range/auth/password" and not "/api/shooting-range/auth/logout" and not "/api/shooting-range/csrf")
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { message = "Password change required." }, context.RequestAborted);
        return;
    }
    await next(context);
});
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();

public partial class Program { }

internal static class StartupConfigurationValidation
{
    internal static void ValidateMockMode(bool useMock, IHostEnvironment environment)
    {
        if (useMock && !environment.IsDevelopment() && !environment.IsEnvironment("Test"))
            throw new InvalidOperationException("ShootingRange mock storage is allowed only in Development or Test.");
    }

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
