using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Utils;
using System.Runtime.CompilerServices;
using System.Security.Authentication;

// https://www.googleapis.com/youtube/v3/videos?part=id,snippet,statistics,&id=WC2sEcEsti8&key=AIzaSyCSFaI1a70I39eF_tlnXWZWTJ49tfyNUWE
// https://www.googleapis.com/youtube/v3/videos?part=id,snippet,suggestions,statistics,contentDetails&id=WC2sEcEsti8&key=AIzaSyCSFaI1a70I39eF_tlnXWZWTJ49tfyNUWE

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration["Config"];
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<DataService>();
if (config == "dev")
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataMockService>();
    builder.Services.AddScoped<IMatchRepository, MatchMockRepository>();
    builder.Services.AddScoped<IProductRepository, ProductMockRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorMockRepository>();
    builder.Services.AddScoped<IPageRepository, PageMockRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventMockRepository>();
    builder.Services.AddScoped<IBioLinkRepository, BioLinkMockRepository>();
}
else
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataService>();
    builder.Services.AddScoped<IMatchRepository, MatchRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorRepository>();
    builder.Services.AddScoped<IPageRepository, PageRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
    builder.Services.AddScoped<IBioLinkRepository, BioLinkRepository>();

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

    builder.Services.AddHttpClient("Youtube", httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/videos");
    });
}

builder.Services.AddSingleton<MyMemoryCache>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var matchCacheKey = "data";
var productCacheKey = "product";
var sponsorCacheKey = "sponsor";
var pagesCacheKey = "pages";
var calendarEventsCacheKey = "calendarEvents";

var apiGroup = app.MapGroup("/api");

apiGroup.MapGet("/reset", ([FromQuery(Name = "k")] string keys, DataService dataService, MyMemoryCache memoryCache) =>
{
    if (string.IsNullOrEmpty(keys))
    {
        memoryCache.Cache.Remove(matchCacheKey);
        memoryCache.Cache.Remove(productCacheKey);
        memoryCache.Cache.Remove(sponsorCacheKey);
        memoryCache.Cache.Remove(pagesCacheKey);
        memoryCache.Cache.Remove(calendarEventsCacheKey);
    }
    foreach (var key in keys.Split(","))
    {
        switch (key)
        {
            case "match":
                memoryCache.Cache.Remove(matchCacheKey);
                break;
            case "product":
                memoryCache.Cache.Remove(productCacheKey);
                break;
            case "sponsor":
                memoryCache.Cache.Remove(sponsorCacheKey);
                break;
            case "page":
                memoryCache.Cache.Remove(pagesCacheKey);
                break;
            case "calendar":
                memoryCache.Cache.Remove(calendarEventsCacheKey);
                break;
            default:
                break;

        }
    }

    return Results.Ok();
})
.WithName("Reset")
.WithOpenApi();

static async Task<IList<Match>> FetchMatches(IExternalDataService dataService, MyMemoryCache memoryCache, string cacheKey)
{
    if (memoryCache.Cache.TryGetValue(cacheKey, out IList<Match>? entities))
    {
        return entities ?? [];
    }
    entities = (await dataService.FetchMatches()).OrderByDescending(x => x.CreationDateTime).ToList();

    memoryCache.Cache.Set(cacheKey, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });

    return entities;
}
apiGroup.MapGet("/matches", (IExternalDataService dataService, MyMemoryCache memoryCache) => FetchMatches(dataService, memoryCache, matchCacheKey))
.WithName("Matches")
.WithOpenApi();

apiGroup.MapGet("/matches/{url}", async (IExternalDataService dataService, MyMemoryCache memoryCache, string url) =>
{
    return (await FetchMatches(dataService, memoryCache, matchCacheKey))?.FirstOrDefault(x => x.Url == url);

})
.WithName("Match Detail")
.WithOpenApi();

apiGroup.MapGet("/pages/{url}", async (DataService dataService, MyMemoryCache memoryCache, string url) =>
{
    if (!memoryCache.Cache.TryGetValue(pagesCacheKey, out IList<Page>? entities))
    {
        entities = await dataService.GetPages();

        memoryCache.Cache.Set(pagesCacheKey, entities.OrderByDescending(x => x.CreationDateTime), new MemoryCacheEntryOptions
        {
            Size = 1
        });
    }
    return entities?.FirstOrDefault(x => x.Url == url);
})
.WithName("Page Detail")
.WithOpenApi();

apiGroup.MapGet("/products", async (DataService dataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(productCacheKey, out IList<Product>? entities))
    {
        return entities;
    }
    entities = await dataService.GetProducts();
    memoryCache.Cache.Set(productCacheKey, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return entities;
})
.WithName("Products")
.WithOpenApi();

apiGroup.MapGet("/sponsors", async (DataService dataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(sponsorCacheKey, out IList<Sponsor>? entities))
    {
        return entities;
    }
    entities = await dataService.GetSponsors();
    memoryCache.Cache.Set(sponsorCacheKey, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return entities;
})
.WithName("Sponsors")
.WithOpenApi();

apiGroup.MapGet("/biolinks", async (DataService dataService, MyMemoryCache memoryCache) =>
{
    var bioLinkCacheKey = "biolinkCache";
    if (memoryCache.Cache.TryGetValue(bioLinkCacheKey, out IList<BioLink>? entities))
    {
        return entities;
    }
    entities = await dataService.GetBioLinks();
    memoryCache.Cache.Set(sponsorCacheKey, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return entities;
})
.WithName("BioLinks")
.WithOpenApi();

apiGroup.MapGet("/calendarEvents", async (DataService dataService, IExternalDataService externalDataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(calendarEventsCacheKey, out IList<CalendarEvent>? entities))
    {
        return entities;
    }
    entities = (await dataService.GetCalendarEvents()).OrderBy(x => x.Date).ToList();

    var matches = await FetchMatches(externalDataService, memoryCache, matchCacheKey);

    entities = entities.Select(entity =>
    {
        var match = matches.FirstOrDefault(x => x.MatchId == entity.MatchId);
        return match == null ? entity : entity with { MatchUrl = match.IsLink ? match.ThumbnailUrl : match.Url };
    }).Where(x => !string.IsNullOrEmpty(x.MatchUrl)).ToList();

    memoryCache.Cache.Set(calendarEventsCacheKey, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });

    return entities;
})
.WithName("CalendarEvents")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

