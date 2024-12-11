using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Utils;
using System.Security.Authentication;

// https://www.googleapis.com/youtube/v3/videos?part=id,snippet,statistics,&id=WC2sEcEsti8&key=AIzaSyCSFaI1a70I39eF_tlnXWZWTJ49tfyNUWE
// https://www.googleapis.com/youtube/v3/videos?part=id,snippet,suggestions,statistics,contentDetails&id=WC2sEcEsti8&key=AIzaSyCSFaI1a70I39eF_tlnXWZWTJ49tfyNUWE

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var config = builder.Configuration["Config"];
var AllowEverythingCORS = "AllowEverything";
var AppCors = "AppCors";
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder =>
        builder.Expire(TimeSpan.FromMinutes(3)));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(AllowEverythingCORS, policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .AllowAnyHeader();
    });

    var siteUrl = builder.Configuration["SiteUrl"];
    if (string.IsNullOrEmpty(siteUrl))
    {
        throw new NullReferenceException(nameof(siteUrl));
    }
    if(config == "dev")
    {
        options.AddPolicy(AppCors, policy =>
        {
            policy.WithOrigins("http://localhost", siteUrl)
                  .WithMethods("GET")
                  .AllowAnyHeader();
        });
    }
    else
    {
        options.AddPolicy(AppCors, policy =>
        {
            policy.WithOrigins(siteUrl)
                  .WithMethods("GET")
                  .AllowAnyHeader();
        });
    }
});

//builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddScoped<DataService>();
if (config == "dev")
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataMockService>();
    builder.Services.AddScoped<IMatchRepository, MatchMockRepository>();
    builder.Services.AddScoped<IProductRepository, ProductMockRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorMockRepository>();
    builder.Services.AddScoped<IPageRepository, PageMockRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventMockRepository>();
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkMockRepository>();
    builder.Services.AddHttpClient();
}
else
{
    builder.Services.AddScoped<IExternalDataService, ExternalDataService>();
    builder.Services.AddScoped<IMatchRepository, MatchRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ISponsorRepository, SponsorRepository>();
    builder.Services.AddScoped<IPageRepository, PageRepository>();
    builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
    builder.Services.AddScoped<IShortLinkRepository, ShortLinkRepository>();

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

app.MapDefaultEndpoints();
app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json","MorWalPiz api"));
}

app.UseHttpsRedirection();
app.UseOutputCache();
app.UseCors(AllowEverythingCORS);

app.UseCors(AppCors);


var apiGroup = app.MapGroup("/api").RequireCors(AppCors).WithOpenApi();

apiGroup.MapGet("/test/{token}",async(IHttpClientFactory httpClientFactory, HttpContext context, string token)=> {
    var client = httpClientFactory.CreateClient();
    var host = context.Request.Host.Value;
    var url = "https://www.google.com/recaptcha/api/siteverify";
    var parameters = new Dictionary<string, string> { { "secret", "1" }, { "response", token }, { "remoteip", host } };
    var encodedContent = new FormUrlEncodedContent(parameters);

    var response = await client.PostAsync(url, encodedContent);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Ok(result);
}).WithName("Test");

app.MapGet("/sl/{videoShortLink}", async (HttpContext context, string videoShortLink, DataService dataService, IExternalDataService extDataService, MyMemoryCache memoryCache) =>
{
    if (string.IsNullOrWhiteSpace(videoShortLink))
        return Results.BadRequest("Video ID is required.");

    if (!memoryCache.Cache.TryGetValue(CacheKeys.ShortLink, out IList<ShortLink>? shortLinks))
    {
        shortLinks = await dataService.FetchShortLinks();

        memoryCache.Cache.Set(CacheKeys.ShortLink, shortLinks, new MemoryCacheEntryOptions
        {
            Size = 1
        });
    }

    var shortLink = shortLinks?.FirstOrDefault(x=> x.Code == videoShortLink);

    if(shortLink == null)
        return Results.BadRequest("shortLink not found");

    var existingMatch = (await FetchMatches(extDataService,memoryCache, CacheKeys.Match)).FirstOrDefault(x => (x.IsLink && x.ThumbnailUrl == shortLink.VideoId) || (x.Videos!= null && x.Videos.Any(v => v.YoutubeId == shortLink.VideoId)));

    if (existingMatch == null)
        return Results.BadRequest("Video not found");

    var videoId = string.Empty;

    if (existingMatch.IsLink)
    {
        videoId= existingMatch.MatchId;
    }
    else
    {
        var selectedVideo = existingMatch.Videos.FirstOrDefault(x => x.YoutubeId == shortLink.VideoId);
        videoId = selectedVideo?.YoutubeId;
        if (selectedVideo == null)
            return Results.BadRequest("Video shortLink not found");
    }

    var linkQuerystring = !string.IsNullOrEmpty(shortLink.QueryString) ? $"&{shortLink.QueryString}" : string.Empty;

    await dataService.UpdateShortlink(shortLink with { ClicksCount = shortLink.ClicksCount++ });

    // Get the User-Agent from the headers
    var userAgent = context.Request.Headers["User-Agent"].ToString();

    // Base fallback URL for web browsers
    string webUrl = $"https://www.youtube.com/watch?v={videoId}{linkQuerystring}";

    // Detect device and set appropriate redirect URL
    if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        return Results.Redirect($"vnd.youtube://watch?v={videoId}{linkQuerystring}");
    else if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
             userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        return Results.Redirect($"youtube://watch?v={videoId}{linkQuerystring}");

    // Fallback to YouTube web URL for unsupported platforms
    return Results.Redirect(webUrl);
}).RequireCors(AllowEverythingCORS).WithName("ShortLink");

apiGroup.MapGet("/purge/{tag}", async (IOutputCacheStore cache, string tag) =>
{
    await cache.EvictByTagAsync(tag, default);
}).WithName("Purge");

apiGroup.MapGet("/reset", ([FromQuery(Name = "k")] string keys, DataService dataService, MyMemoryCache memoryCache) =>
{
    if (string.IsNullOrEmpty(keys))
        keys = $"{CacheKeys.Match},{CacheKeys.Product},{CacheKeys.Sponsor},{CacheKeys.Pages},{CacheKeys.CalendarEvents}";

    foreach (var key in keys.ToLower().Split(","))
        memoryCache.Cache.Remove(key);

    return Results.Ok();
})
.WithName("Reset");

static async Task<IList<Match>> FetchMatches(IExternalDataService dataService, MyMemoryCache memoryCache, string cacheKey)
{
    if (memoryCache.Cache.TryGetValue(cacheKey, out IList<Match>? entities))
        return entities ?? [];

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
        return entities;
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

