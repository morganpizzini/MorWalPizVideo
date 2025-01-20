using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var config = builder.Configuration["Config"];

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder =>
        builder.Expire(TimeSpan.FromMinutes(10)));
});

builder.Services.AddOpenApi();

builder.Services.AddScoped<DataService>();
// Read configuration from appsettings.json
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection("BlobStorage"));
if (config == "dev")
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataMockService>();
    builder.Services.AddScoped<IMatchRepository, MatchMockRepository>();
    builder.Services.AddScoped<IProductRepository, ProductMockRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorMockRepository>();
    builder.Services.AddScoped<IPageRepository, PageMockRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventMockRepository>();
    builder.Services.AddScoped<ISponsorApplyRepository, SponsorApplyMockRepository>();
    builder.Services.AddScoped<IBioLinkRepository, BioLinkMockRepository>();
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkMockRepository>();
    builder.Services.AddScoped<IYTChannelRepository, YTChannelMockRepository>();
    builder.Services.AddScoped<IYTService, YTServiceMock>();

    builder.Services.AddScoped<IBlobService, BlobServiceMock>();
}
else
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataService>();
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


    builder.Services.AddScoped<IBlobService, BlobService>();

    MorWalPizDatabaseSettings? dbConfig = builder.Configuration.GetSection("MorWalPizDatabase").Get<MorWalPizDatabaseSettings>();

    if (dbConfig == null)
    {
        throw new Exception("Cannot read configuration for MongoDB");
    }

    MongoClientSettings settings = MongoClientSettings.FromUrl(
        new MongoUrl(dbConfig.ConnectionString)
    );
    settings.SslSettings =
    new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

    builder.Services.AddScoped(s =>
        new MongoClient(settings).GetDatabase(dbConfig.DatabaseName));

    builder.Services.AddHttpClient(HttpClientNames.Recaptcha, httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://www.google.com/recaptcha/api/siteverify");
    });
    builder.Services.AddHttpClient(HttpClientNames.YouTube, httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/videos");
    });
}

builder.Services.AddSingleton<MyMemoryCache>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MorWalPiz API"));
}

app.UseHttpsRedirection();
if(!app.Environment.IsDevelopment())
    app.UseOutputCache();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

