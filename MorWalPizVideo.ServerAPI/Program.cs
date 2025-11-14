using Azure.Identity;
using Microsoft.FeatureManagement;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
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
var enableOutputCache = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableOutputCache);
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
            Console.WriteLine($"Warning: Could not connect to KeyVault at {keyVaultUrl}: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Warning: EnableKeyVault is true but KeyVaultUrl is not configured");
    }
}

builder.AddServiceDefaults();

if (enableCache)
{
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(builder =>
            builder.Expire(TimeSpan.FromMinutes(10)));
    });
}


if (enableSwagger)
    builder.Services.AddOpenApi();

builder.Services.AddScoped<IGenericDataService,DataService>();

if (enableMock)
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataMockService>();
    builder.Services.AddScoped<IYouTubeContentRepository, MatchMockRepository>();
    builder.Services.AddScoped<IProductRepository, ProductMockRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorMockRepository>();
    builder.Services.AddScoped<IPageRepository, PageMockRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventMockRepository>();
    builder.Services.AddScoped<ISponsorApplyRepository, SponsorApplyMockRepository>();
    builder.Services.AddScoped<IBioLinkRepository, BioLinkMockRepository>();
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkMockRepository>();
    builder.Services.AddScoped<IYTChannelRepository, YTChannelMockRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryMockRepository>();
    builder.Services.AddScoped<IQueryLinkRepository, QueryLinkMockRepository>();
    builder.Services.AddScoped<IPublishScheduleRepository, PublishScheduleMockRepository>();
    builder.Services.AddScoped<IConfigurationRepository, ConfigurationMockRepository>();

    builder.Services.AddScoped<IYTService, YTServiceMock>();
    builder.Services.AddScoped<IBlobService, BlobServiceMock>();
}
else
{
    BsonSerializer.RegisterSerializer(typeof(object), new MorWalPizVideo.Server.Models.Serializers.ObjectWithJsonElementSerializer());
    builder.Services.AddScoped<IExternalDataService, ExternalDataService>();
    builder.Services.AddScoped<IYouTubeContentRepository, YouTubeContentRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorRepository>();
    builder.Services.AddScoped<ISponsorApplyRepository, SponsorApplyRepository>();
    builder.Services.AddScoped<IPageRepository, PageRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
    builder.Services.AddScoped<IBioLinkRepository, BioLinkRepository>();
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
    builder.Services.AddScoped<IYTChannelRepository, YTChannelRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IQueryLinkRepository, QueryLinkRepository>();
    builder.Services.AddScoped<IPublishScheduleRepository, PublishScheduleRepository>();
    builder.Services.AddScoped<IYTService, YTService>();
    builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
    builder.Services.AddScoped<ITranslatorService, TranslatorServiceMock>();

    builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("BlobStorage"));
    builder.Services.AddScoped<IBlobService, BlobService>();

    // Configure MongoDB using Options pattern with lazy loading
    // This ensures Azure Key Vault configuration is fully loaded before accessing database settings
    builder.Services.Configure<MorWalPizDatabaseSettings>(
        builder.Configuration.GetSection("MorWalPizDatabase"));
    
    builder.Services.AddScoped<IMongoDbService, MongoDbService>();
    builder.Services.AddScoped<IMongoDatabase>(provider => 
        provider.GetRequiredService<IMongoDbService>().GetDatabase());

    builder.Services.AddHttpClient(HttpClientNames.Recaptcha, httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://www.google.com/recaptcha/api/siteverify");
    });
    builder.Services.AddHttpClient(HttpClientNames.YouTube, httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/videos");
    });
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

builder.Services.AddControllers();

// Add health checks
builder.Services.AddHealthChecks();

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

// Map health check endpoint
app.MapHealthChecks("/health");

app.UseDefaultFiles();
app.MapStaticAssets();

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

if (enableOutputCache)
    app.UseOutputCache();

app.MapControllers();


app.MapFallbackToFile("/index.html");


app.Run();
