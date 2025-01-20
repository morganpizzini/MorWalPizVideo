using Hangfire;
using MongoDB.Driver;
using MorWalPizVideo.BackOffice.Jobs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;
using Hangfire.MemoryStorage;
using System.Net.Http.Headers;
using System.Security.Authentication;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();

var dbConfig = builder.Configuration.GetSection("MorWalPizDatabase").Get<MorWalPizDatabaseSettings>();

if (dbConfig == null)
    throw new Exception("Cannot read configuration for MongoDB");

var telegramSettings = builder.Configuration.GetSection("TelegramSettings").Get<TelegramSettings>();

if (telegramSettings == null)
    throw new Exception("Cannot read configuration for Telegram");

var discordSettings = builder.Configuration.GetSection("DiscordSettings").Get<TelegramSettings>();

if (discordSettings == null)
    throw new Exception("Cannot read configuration for Discord");

MongoClientSettings settings = MongoClientSettings.FromUrl(
    new MongoUrl(dbConfig.ConnectionString)
);
settings.SslSettings =
    new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

builder.Services.AddScoped(s =>
    new MongoClient(settings).GetDatabase(dbConfig.DatabaseName));

var siteUrl = $"{builder.Configuration["SiteUrl"]}api/";

builder.Services.AddHttpClient(HttpClientNames.MorWalPiz, httpClient =>
{
    httpClient.BaseAddress = new Uri(siteUrl);
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

// Aggiungi HttpClient
builder.Services.AddHttpClient(HttpClientNames.Discord, client =>
{
    client.BaseAddress = new Uri("https://discord.com/api/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bot", discordSettings.Token);
});

builder.Services.AddHttpClient(HttpClientNames.YouTube, httpClient =>
{
    httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/videos");
});

builder.Services.AddHttpClient(HttpClientNames.Telegram, httpClient =>
{
    httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{telegramSettings.Token}/sendMessage");
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});


var translatorSettings = builder.Configuration.GetSection("TranslatorSettings").Get<TranslatorSettings>();

if(translatorSettings == null)
    throw new Exception("Cannot read configuration for Translator");


builder.Services.AddScoped<TranslatorService>((c) => { return new TranslatorService(translatorSettings.SubscriptionKey, translatorSettings.Endpoint, translatorSettings.Region); });
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISponsorRepository, SponsorRepository>();
builder.Services.AddScoped<ISponsorApplyRepository, SponsorApplyRepository>();
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
builder.Services.AddScoped<IBioLinkRepository, BioLinkRepository>();
builder.Services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
builder.Services.AddScoped<IYTChannelRepository, YTChannelRepository>();

builder.Services.AddScoped<IYTService, YTService>();
builder.Services.AddScoped<IDiscordService, DiscordService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IBlobService, BlobServiceMock>();
}
else
{
    builder.Services.Configure<BlobStorageOptions>(
        builder.Configuration.GetSection("BlobStorage"));
    builder.Services.AddScoped<IBlobService, BlobService>();
}

builder.Services.AddOpenApi();

// Configure Hangfire services
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseDefaultTypeSerializer();

    if(!builder.Environment.IsDevelopment())
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


var app = builder.Build();

// Use Hangfire Dashboard (accessible via /hangfire)
app.UseHangfireDashboard();

// Schedule a recurring job
RecurringJob.AddOrUpdate<NewsJobs>(
    "news-job",            // Job ID
    job => job.ExecuteAsync(), // The method to run
    "0 18 * * 0"              // Cron expression: Sunday at 18:00
);

app.MapDefaultEndpoints();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MorWalPiz backoffice API"));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
