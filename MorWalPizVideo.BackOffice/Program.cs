using Azure.Identity;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MorWalPizVideo.BackOffice.Jobs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Configuration;
using MorWalPizVideo.BackOffice.Services.Factories;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain; // Assicurati che questo using sia presente
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Utils;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var featureFlags = builder.Configuration.GetSection("FeatureManagement");
builder.Services.AddFeatureManagement()
    .UseDisabledFeaturesHandler(new DisabledFeaturesHandler());

var enableHangFire = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableHangFire);
var enableSwagger = builder.Configuration.IsFeatureEnabled(MyFeatureFlags.EnableSwagger);
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

// Configure comprehensive health checks
builder.Services.ConfigureHealthChecks(builder.Configuration);

// Enable CORS for all in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}


builder.Services.Configure<AzureConfig>(builder.Configuration.GetSection("AzureConfig"));

builder.Services.AddSingleton<IChatCompletionService>(sp =>
{
    AzureConfig options = sp.GetRequiredService<IOptions<AzureConfig>>().Value;

    return new AzureOpenAIChatCompletionService(
        options.OpenAi.DeploymentName,
        options.OpenAi.OpenAiEndpoint,
        options.OpenAi.OpenAiKey);
});
builder.Services.AddTransient<Kernel>();
// Add services to the container.

builder.Services.AddControllers();

if (!enableMock)
{
    // Configure options for lazy loading
    //builder.Services.Configure<TelegramSettings>("TelegramSettings", builder.Configuration.GetSection("TelegramSettings"));
    //builder.Services.Configure<TelegramSettings>("DiscordSettings", builder.Configuration.GetSection("DiscordSettings"));
    
    // Register configuration services for lazy loading
    builder.Services.AddScoped<IDiscordConfigurationService, DiscordConfigurationService>();
    builder.Services.AddScoped<ITelegramConfigurationService, TelegramConfigurationService>();
    
    // Register HttpClient factories
    builder.Services.AddScoped<IDiscordHttpClientFactory, DiscordHttpClientFactory>();
    builder.Services.AddScoped<ITelegramHttpClientFactory, TelegramHttpClientFactory>();


    var siteUrl = $"{builder.Configuration["SiteUrl"]}api/";

    builder.Services.AddHttpClient(HttpClientNames.MorWalPiz, httpClient =>
    {
        httpClient.BaseAddress = new Uri(siteUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    });

    builder.Services.AddHttpClient(HttpClientNames.YouTube, httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/videos");
    });

    // Note: Discord and Telegram HttpClients will be created via factories when needed
}


builder.Services.AddScoped<DataService>();
if (enableMock)
{
    var siteUrl = $"{builder.Configuration["SiteUrl"]}api/";

    builder.Services.AddHttpClient(HttpClientNames.MorWalPiz, httpClient =>
    {
        httpClient.BaseAddress = new Uri(siteUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    });
    builder.Services.AddScoped<IYTService, YTServiceMock>();

    builder.Services.AddScoped<IYouTubeContentRepository, MatchMockRepository>();
    builder.Services.AddScoped<IProductRepository, ProductMockRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorMockRepository>();
    builder.Services.AddScoped<ISponsorApplyRepository, SponsorApplyMockRepository>();
    builder.Services.AddScoped<IPageRepository, PageMockRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventMockRepository>();
    builder.Services.AddScoped<IBioLinkRepository, BioLinkMockRepository>();
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkMockRepository>();
    builder.Services.AddScoped<IYTChannelRepository, YTChannelMockRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryMockRepository>();
    builder.Services.AddScoped<IQueryLinkRepository, QueryLinkMockRepository>();
    builder.Services.AddScoped<IPublishScheduleRepository, PublishScheduleMockRepository>();
    builder.Services.AddScoped<IConfigurationRepository, ConfigurationMockRepository>(); // Aggiungi questa linea
    // services
    //builder.Services.AddScoped<IYTService, YTServiceMock>();
    builder.Services.AddScoped<IDiscordService, DiscordServiceMock>();
    builder.Services.AddScoped<ITelegramService, TelegramServiceMock>();
    builder.Services.AddScoped<IBlobService, BlobServiceMock>();
    builder.Services.AddScoped<IImageGenerationService, ImageGenerationService>();
}
else
{
    BsonSerializer.RegisterSerializer(typeof(object), new MorWalPizVideo.Server.Models.Serializers.ObjectWithJsonElementSerializer());

    builder.Services.Configure<MorWalPizDatabaseSettings>(
        builder.Configuration.GetSection("MorWalPizDatabase"));

    builder.Services.AddScoped<IMongoDbService, MongoDbService>();
    builder.Services.AddScoped<IMongoDatabase>(provider =>
        provider.GetRequiredService<IMongoDbService>().GetDatabase());

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
    builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>(); // Aggiungi questa linea

    builder.Services.AddScoped<DataService>();
    builder.Services.AddScoped<IYTService, YTService>();
    // services
    builder.Services.AddScoped<IDiscordService, DiscordService>();
    builder.Services.AddScoped<ITelegramService, TelegramService>();
    builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("BlobStorage"));
    builder.Services.AddScoped<IBlobService, BlobService>();
    builder.Services.AddScoped<IImageGenerationService, ImageGenerationService>();
}

if (enableSwagger)
    builder.Services.AddOpenApi();

if (enableHangFire)
{
    // Configure Hangfire services
    builder.Services.AddHangfire(config =>
    {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
              .UseSimpleAssemblyNameTypeSerializer()
              .UseDefaultTypeSerializer();

        if (!builder.Environment.IsDevelopment())
        {
            // run dotnet ef database update
            config.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"));
        }
        else
        {
            config.UseMemoryStorage();
        }
    });

    // Add Hangfire Server (Background Worker)
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

if (enableHangFire)
{
    // Use Hangfire Dashboard (accessible via /hangfire)
    app.UseHangfireDashboard();

    // Schedule a recurring job
    RecurringJob.AddOrUpdate<NewsJobs>(
        "news-job",            // Job ID
        job => job.ExecuteAsync(), // The method to run
        "0 18 * * 0"              // Cron expression: Sunday at 18:00
    );
}

if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

app.MapDefaultEndpoints();

if (enableSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MorWalPiz backoffice API"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
