using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using System.Security.Claims;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddFeatureManagement();
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
var migrateDatabase = args.Contains("--migrate-database", StringComparer.Ordinal);
var useInMemoryDb = !migrateDatabase && await StartupConfigurationValidation.IsFeatureEnabledAsync(
    builder.Configuration, StartupConfigurationValidation.EnableInMemoryDb);
var useMock = !migrateDatabase && await StartupConfigurationValidation.IsFeatureEnabledAsync(builder.Configuration, StartupConfigurationValidation.UseMock);
if (useInMemoryDb)
    useMock = false;
StartupConfigurationValidation.ValidateMockMode(useMock, builder.Environment);
StartupConfigurationValidation.ValidateInMemoryDbMode(useInMemoryDb, builder.Environment);
var enableKeyVault = await StartupConfigurationValidation.IsFeatureEnabledAsync(builder.Configuration, StartupConfigurationValidation.EnableKeyVault);
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

if (!useMock && !useInMemoryDb)
    StartupConfigurationValidation.ValidateSqlServerConfiguration(builder.Configuration, enableKeyVault ? keyVaultProvider : null);

if (useInMemoryDb)
{
    builder.Services.AddDbContext<ShootingRangeDbContext>(options => options.UseInMemoryDatabase("ShootingRange"));
    builder.Services.AddSingleton(new SemaphoreSlim(1, 1));
    builder.Services.AddScoped<IShootingRangeRepositorySet, SqlServerShootingRangeRepositorySet>();
}
else if (useMock) builder.Services.AddSingleton<IShootingRangeRepositorySet, MockShootingRangeRepositorySet>();
else
{
    var connectionString = builder.Configuration["ShootingRangeDatabase:ConnectionString"]!;
    if (migrateDatabase)
        connectionString = builder.Configuration["ShootingRangeDatabase:MigrationConnectionString"] ?? connectionString;
    builder.Services.AddDbContext<ShootingRangeDbContext>(options => options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IShootingRangeRepositorySet, SqlServerShootingRangeRepositorySet>();
    builder.Services.AddHealthChecks().AddCheck<ShootingRangeSqlServerReadiness>("range-sqlserver", tags: ["ready", "startup"]);
}
builder.Services.AddScoped<ShootingRangeService>();
var app = builder.Build();
if (useInMemoryDb)
{
    var adminPassword = builder.Configuration["ShootingRange:InMemoryAdminPassword"];
    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        app.Logger.LogWarning("No in-memory administrator was seeded because ShootingRange:InMemoryAdminPassword is not configured.");
    }
    else
    {
        using var scope = app.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        if (await repositories.Users.FindByUsernameAsync("admin") is null)
        {
            var hash = PasswordHashing.HashPassword(adminPassword, out var salt);
            await repositories.Users.InsertAsync(new()
            {
                Id = "in-memory-admin",
                Username = "admin",
                FirstName = "Local",
                LastName = "Administrator",
                PasswordHash = $"{hash}:{salt}",
                Status = MorWalPizVideo.ShootingRange.Models.ShootingRangeAccountStatus.Approved,
                IsAdmin = true,
                ForcePasswordChange = true,
                SecurityVersion = Guid.NewGuid().ToString("N")
            });
            app.Logger.LogInformation("Seeded the in-memory administrator account.");
        }
    }
}

if (!useMock && !useInMemoryDb)
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<ShootingRangeDbContext>().Database;
    if (migrateDatabase)
    {
        await database.MigrateAsync();
        return;
    }

    await scope.ServiceProvider.GetRequiredService<SqlServerShootingRangeRepositorySet>().VerifyReadyAsync(CancellationToken.None);
    app.Logger.LogInformation("Shooting range SQL Server schema preflight succeeded.");
}
app.MapDefaultEndpoints();
app.UseCors();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (Exception error) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested && error is ArgumentException or KeyNotFoundException or UnauthorizedAccessException or InvalidOperationException or DbUpdateException or SqlException or OperationCanceledException)
    {
        var uniqueViolation = StartupConfigurationValidation.IsUniqueConstraintViolation(error);
        var status = error switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ when uniqueViolation => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status503ServiceUnavailable
        };
        app.Logger.LogWarning("Shooting range request rejected with {StatusCode} ({ErrorType}).", status, error.GetType().Name);
        context.Response.StatusCode = status;
        var message = status == 503
            ? "Transactional storage is unavailable. Refresh the current state before retrying."
            : uniqueViolation
                ? "The selected value conflicts with an existing record."
                : error.Message;
        await context.Response.WriteAsJsonAsync(new { message }, context.RequestAborted);
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
    internal const string EnableInMemoryDb = "EnableInMemoryDb";
    internal const string UseMock = "UseMock";
    internal const string EnableKeyVault = "EnableKeyVault";

    internal static async Task<bool> IsFeatureEnabledAsync(IConfiguration configuration, string featureName)
    {
        var featureServices = new ServiceCollection();
        featureServices.AddFeatureManagement(configuration);
        using var provider = featureServices.BuildServiceProvider();
        return await provider.GetRequiredService<IFeatureManager>().IsEnabledAsync(featureName);
    }

    internal static void ValidateMockMode(bool useMock, IHostEnvironment environment)
    {
        if (useMock && !environment.IsDevelopment() && !environment.IsEnvironment("Test"))
            throw new InvalidOperationException("ShootingRange mock storage is allowed only in Development or Test.");
    }

    internal static void ValidateInMemoryDbMode(bool useInMemoryDb, IHostEnvironment environment)
    {
        if (useInMemoryDb && !environment.IsDevelopment() && !environment.IsEnvironment("Test"))
            throw new InvalidOperationException("ShootingRange in-memory database is allowed only in Development or Test.");
    }

    internal static void HandleKeyVaultFailure(string message, bool useMock, IHostEnvironment environment, Exception? innerException = null)
    {
        if (!useMock && !environment.IsDevelopment())
            throw new InvalidOperationException(message, innerException);

        Console.WriteLine($"Warning: {message}");
    }

    internal static void ValidateSqlServerConfiguration(IConfiguration configuration, IConfigurationProvider? keyVaultProvider)
    {
        const string connectionStringKey = "ShootingRangeDatabase:ConnectionString";

        var missing = string.IsNullOrWhiteSpace(configuration[connectionStringKey]) ||
            (keyVaultProvider is not null && (!keyVaultProvider.TryGet(connectionStringKey, out var value) || string.IsNullOrWhiteSpace(value)));

        if (missing)
            throw new InvalidOperationException($"Required Azure SQL configuration is missing: {connectionStringKey}.");
    }

    internal static bool IsUniqueConstraintViolation(Exception error) => error switch
    {
        SqlException { Number: 2601 or 2627 } => true,
        DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => true,
        _ => false
    };
}
