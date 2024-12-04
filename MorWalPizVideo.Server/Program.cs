using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
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
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder =>
        builder.Expire(TimeSpan.FromMinutes(3)));
});
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
}
else
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataService>();
    builder.Services.AddScoped<IMatchRepository, MatchRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorRepository>();
    builder.Services.AddScoped<IPageRepository, PageRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();

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
app.UseOutputCache();

var apiGroup = app.MapGroup("/api").WithOpenApi();

apiGroup.MapGet("/purge/{tag}", async (IOutputCacheStore cache, string tag) =>
{
    await cache.EvictByTagAsync(tag, default);
}).WithName("Purge");

apiGroup.MapGet("/reset", ([FromQuery(Name = "k")] string keys, DataService dataService, MyMemoryCache memoryCache) =>
{
    if (string.IsNullOrEmpty(keys))
        keys = $"{CacheKeys.Match},{CacheKeys.Product},{CacheKeys.Sponsor},{CacheKeys.Pages},{CacheKeys.CalendarEvents}";

    foreach (var key in keys.ToLower().Split(","))
    {
        memoryCache.Cache.Remove(key);
    }
    return Results.Ok();
})
.WithName("Reset");

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

var matchGroup = apiGroup.MapGroup("matches")
    .CacheOutput(builder => builder.Tag(ApiTagCacheKeys.Matches));

matchGroup.MapGet("/", (IExternalDataService dataService, MyMemoryCache memoryCache) => FetchMatches(dataService, memoryCache, CacheKeys.Match))
.WithName("Matches");

matchGroup.MapGet("/{url}", async (IExternalDataService dataService, MyMemoryCache memoryCache, string url) =>
{
    return (await FetchMatches(dataService, memoryCache, CacheKeys.Match))?.FirstOrDefault(x => x.Url == url);

})
.WithName("Match Detail");

apiGroup.MapGet("/pages/{url}", async (DataService dataService, MyMemoryCache memoryCache, string url) =>
{
    if (!memoryCache.Cache.TryGetValue(CacheKeys.Pages, out IList<Page>? entities))
    {
        entities = await dataService.GetPages();

        memoryCache.Cache.Set(CacheKeys.Pages, entities.OrderByDescending(x => x.CreationDateTime), new MemoryCacheEntryOptions
        {
            Size = 1
        });
    }
    return entities?.FirstOrDefault(x => x.Url == url);
})
.WithName("Page Detail")
.CacheOutput(builder => builder.Tag(ApiTagCacheKeys.Pages));

apiGroup.MapGet("/products", async (DataService dataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(CacheKeys.Product, out IList<Product>? entities))
    {
        return entities;
    }
    entities = await dataService.GetProducts();
    memoryCache.Cache.Set(CacheKeys.Product, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return entities;
})
.WithName("Products")
.CacheOutput(builder => builder.Tag(ApiTagCacheKeys.Products));

apiGroup.MapGet("/sponsors", async (DataService dataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(CacheKeys.Sponsor, out IList<Sponsor>? entities))
    {
        return entities;
    }
    entities = await dataService.GetSponsors();
    memoryCache.Cache.Set(CacheKeys.Sponsor, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return entities;
})
.WithName("Sponsors")
.CacheOutput(builder => builder.Tag(ApiTagCacheKeys.Sponsors));

apiGroup.MapGet("/calendarEvents", async (DataService dataService, IExternalDataService externalDataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(CacheKeys.CalendarEvents, out IList<CalendarEvent>? entities))
    {
        return entities;
    }
    entities = (await dataService.GetCalendarEvents()).OrderBy(x => x.Date).ToList();

    var matches = await FetchMatches(externalDataService, memoryCache, CacheKeys.Match);

    entities = entities.Select(entity =>
    {
        var match = matches.FirstOrDefault(x => x.MatchId == entity.MatchId);
        return match == null ? entity : entity with { MatchUrl = match.IsLink ? match.ThumbnailUrl : match.Url };
    }).Where(x => !string.IsNullOrEmpty(x.MatchUrl)).ToList();

    memoryCache.Cache.Set(CacheKeys.CalendarEvents, entities, new MemoryCacheEntryOptions
    {
        Size = 1
    });

    return entities;
})
.WithName("CalendarEvents")
.CacheOutput(builder => builder.Tag(ApiTagCacheKeys.CalendarEvents));

app.MapFallbackToFile("/index.html");

app.Run();

