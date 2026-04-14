using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.FeatureManagement;
using MongoDB.Driver;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Utils;

var builder = WebApplication.CreateBuilder(args);

var featureFlags = builder.Configuration.GetSection("FeatureManagement");
builder.Services.AddFeatureManagement()
    .UseDisabledFeaturesHandler(new DisabledFeaturesHandler());

var enableDev = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableDev);
var enableSwagger = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableSwagger);
var enableCache = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableCache);
var enableMock = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableMock);
var enableKeyVault = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableKeyVault);

// Configure Azure KeyVault if enabled
if (enableKeyVault)
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        try
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());
        }
        catch (Exception ex)
        {
            // Log the exception and continue without KeyVault
            // This allows the application to start even if KeyVault is unavailable
            throw new Exception($"Warning: Could not connect to KeyVault at {keyVaultUrl}: {ex.Message}");
        }
    }
    else
    {
        throw new Exception("Warning: EnableKeyVault is true but KeyVaultUrl is not configured");

    }
}

builder.AddServiceDefaults();

if (enableSwagger)
    builder.Services.AddOpenApi();

builder.Services.AddScoped<IShortLinkDataService, ShortlinkDataService>();
if (enableMock)
{
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkMockRepository>();
    builder.Services.AddScoped<IYouTubeContentRepository, MatchMockRepository>();
    builder.Services.AddScoped<IYTChannelRepository, YTChannelMockRepository>();
}
else
{
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
    builder.Services.AddScoped<IYouTubeContentRepository, YouTubeContentRepository>();
    builder.Services.AddScoped<IYTChannelRepository, YTChannelRepository>();

    // Configure MongoDB using Options pattern with lazy loading
    // This ensures Azure Key Vault configuration is fully loaded before accessing database settings
    builder.Services.Configure<MorWalPizDatabaseSettings>(
        builder.Configuration.GetSection("MorWalPizDatabase"));

    builder.Services.AddScoped<IMongoDbService, MongoDbService>();
    builder.Services.AddScoped<IMongoDatabase>(provider =>
        provider.GetRequiredService<IMongoDbService>().GetDatabase());

    //builder.Services.AddHttpClient(HttpClientNames.YouTube, httpClient =>
    //{
    //    httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/videos");
    //});
}

if (enableCache)
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddScoped<IMorWalPizCache, MorWalPizMemoryCache>();
}
else
{
    builder.Services.AddSingleton<IMorWalPizCache, MorWalPizMemoryCacheMock>();
}

builder.Services.AddAuthentication("FakeScheme")
    .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>("FakeScheme", options => { });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();


if (enableDev)
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(options =>
    {
        options.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Something went wrong.");
        });
    });
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (enableSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MorWalPiz API"));
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
