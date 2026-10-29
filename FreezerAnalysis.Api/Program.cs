using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Domain.FreezerAnalysis;
using MorWalPizVideo.MvcHelpers.Authentication;
using FreezerAnalysis.Api.Security;
using FreezerAnalysis.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var useMockRepository = builder.Configuration.GetValue("FreezerAnalysis:UseMockRepository", false);
var enableDevelopmentAuthentication = builder.Configuration.GetValue("FreezerAnalysis:EnableDevAuthentication", false);
if ((useMockRepository || enableDevelopmentAuthentication) &&
    !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Test"))
{
    throw new InvalidOperationException("FreezerAnalysis mocks and development authentication are allowed only in Development or Test.");
}

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("freezer-spa", policy =>
{
    policy.WithMethods("GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS")
        .WithHeaders("Content-Type", "Accept", PublicAccountSecurity.CsrfHeader)
        .AllowCredentials();
    if (builder.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var value) && value.IsLoopbackHost());
    }
    else
    {
        var allowedOrigins = builder.Configuration.GetSection("FreezerAnalysis:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins);
    }
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("freezer-account-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

if (enableDevelopmentAuthentication &&
    (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test")))
{
    builder.Services.AddAuthentication(FreezerDevelopmentAuthenticationHandler.AuthenticationScheme)
        .AddScheme<AuthenticationSchemeOptions, FreezerDevelopmentAuthenticationHandler>(
            FreezerDevelopmentAuthenticationHandler.AuthenticationScheme, _ => { });
}
else
{
    FreezerAnalysisMongoRegistration.AddRepositories(builder.Services, builder.Configuration);
    builder.Services.AddAuthentication(PublicAccountSecurity.Scheme)
        .AddPublicAccountAuthentication(PublicAccountSecurity.Scheme, "freezer_account_session");
}

if (useMockRepository)
{
    builder.Services.AddSingleton<IFreezerAnalysisRepository, InMemoryFreezerAnalysisRepository>();
    builder.Services.AddSingleton<IPublicAccountService, UnavailablePublicAccountService>();
}

builder.Services.AddFreezerPrivateObjectStorage(builder.Configuration, builder.Environment);

builder.Services.Configure<FreezerAnalysisHostOptions>(builder.Configuration.GetSection("FreezerAnalysis"));
builder.Services.AddSingleton<FreezerCsvParser>();
builder.Services.AddSingleton<PdfTextExtractor>();
builder.Services.AddSingleton<FreezerPdfReportDetector>();
builder.Services.AddSingleton<FreezerPdfPairParser>();
builder.Services.AddSingleton<FreezerDocumentRenderer>();
builder.Services.AddScoped<FreezerAnalysisCoordinator>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("freezer-spa");
app.UseMiddleware<FreezerCsrfMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
        context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "The requested endpoint does not exist.",
            Status = StatusCodes.Status404NotFound
        });
        return;
    }

    var indexPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
});

app.Run();

public partial class Program { }

public sealed class FreezerAnalysisHostOptions
{
    public int MaximumUploadBytes { get; set; } = 20 * 1024 * 1024;
    public string PdfTextExtractorPath { get; set; } = "pdftotext";
    public bool ApplyIndexesOnStartup { get; set; }
}

public sealed class UnavailablePublicAccountService : IPublicAccountService
{
    public Task<PublicAccountAuthenticationResult?> RegisterAsync(PublicAccountRegistrationCommand request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Platform account services are not configured in mock mode.");
    public Task<PublicAccountAuthenticationResult?> LoginAsync(PublicAccountLoginCommand request, CancellationToken cancellationToken = default) =>
        Task.FromResult<PublicAccountAuthenticationResult?>(null);
    public Task<PublicAccountSessionResult?> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default) =>
        Task.FromResult<PublicAccountSessionResult?>(null);
    public Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<MorWalPizVideo.Models.Models.PublicAccount?> UpdateProfileAsync(string accountId, PublicAccountProfileUpdateCommand request, CancellationToken cancellationToken = default) =>
        Task.FromResult<MorWalPizVideo.Models.Models.PublicAccount?>(null);
    public Task<MorWalPizVideo.Models.Models.PublicAccount?> GetAccountAsync(string accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MorWalPizVideo.Models.Models.PublicAccount?>(null);
}

public sealed class FreezerCsrfMiddleware(RequestDelegate next, IWebHostEnvironment environment, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
            context.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
        {
            var csrfValid = PublicAccountSecurity.HasValidCsrf(
                context.Request,
                "freezer_account_csrf",
                PublicAccountSecurity.CsrfHeader);
            var originValid = IsAllowedOrigin(context);
            if (!csrfValid || !originValid)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "The request origin or CSRF token is invalid.",
                    Status = StatusCodes.Status403Forbidden
                });
                return;
            }
        }

        await next(context);
    }

    private bool IsAllowedOrigin(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Origin", out var origin))
            return true;
        if (!Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var supplied))
            return false;
        var requestOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
        if (string.Equals(supplied.GetLeftPart(UriPartial.Authority), requestOrigin, StringComparison.OrdinalIgnoreCase))
            return true;

        if (environment.IsDevelopment())
            return supplied.IsLoopbackHost();

        return configuration.GetSection("FreezerAnalysis:AllowedOrigins").Get<string[]>()
            ?.Any(allowed => string.Equals(allowed, supplied.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) == true;
    }
}

internal static class UriValidationExtensions
{
    public static bool IsLoopbackHost(this Uri uri) =>
        uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
}
