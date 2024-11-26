using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.Net.Http;
using System.Text.Json;
using System.Web;

// https://www.googleapis.com/youtube/v3/videos?part=id,snippet,statistics,&id=WC2sEcEsti8&key=AIzaSyCSFaI1a70I39eF_tlnXWZWTJ49tfyNUWE
// https://www.googleapis.com/youtube/v3/videos?part=id,snippet,suggestions,statistics,contentDetails&id=WC2sEcEsti8&key=AIzaSyCSFaI1a70I39eF_tlnXWZWTJ49tfyNUWE

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<ExternalDataService>();
builder.Services.AddSingleton<MyMemoryCache>();

builder.Services.AddHttpClient("Youtube", httpClient =>
{
    httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/videos");

    
});

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

var apiGroup = app.MapGroup("/api");

apiGroup.MapGet("/reset", (DataService dataService, MyMemoryCache memoryCache) =>
{
    memoryCache.Cache.Remove("data");
    memoryCache.Cache.Remove("products");
    memoryCache.Cache.Remove("sponsors");
    return Results.Ok();
})
.WithName("Reset")
.WithOpenApi();

var matchCacheKey = "data";
var productCacheKey = "product";
var sponsorCacheKey = "sponsor";

apiGroup.MapGet("/matches", async (ExternalDataService dataService, MyMemoryCache memoryCache) =>
{
    if(memoryCache.Cache.TryGetValue(matchCacheKey, out IList<Match>? matches))
    {
        return matches;
    }
    matches = await dataService.FetchMatches();

    memoryCache.Cache.Set(matchCacheKey, matches.OrderByDescending(x=>x.CreationDateTime), new MemoryCacheEntryOptions
    {
        Size = 1
    });
    
    return matches;
})
.WithName("Matches")
.WithOpenApi();

apiGroup.MapGet("/matches/{url}", async (ExternalDataService dataService, MyMemoryCache memoryCache, string url) =>
{
    if (!memoryCache.Cache.TryGetValue(matchCacheKey, out IList<Match>? matches))
    {
        matches = await dataService.FetchMatches();

        memoryCache.Cache.Set(matchCacheKey, matches.OrderByDescending(x => x.CreationDateTime), new MemoryCacheEntryOptions
        {
            Size = 1
        });
    }
    return matches?.FirstOrDefault(x => x.Url == url);
})
.WithName("Match Detail")
.WithOpenApi();

apiGroup.MapGet("/products", (DataService dataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(productCacheKey, out IList<Product>? products))
    {
        return products;
    }
    products = dataService.GetProducts();
    memoryCache.Cache.Set(productCacheKey, products, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return products;
})
.WithName("Products")
.WithOpenApi();

apiGroup.MapGet("/sponsors", (DataService dataService, MyMemoryCache memoryCache) =>
{
    if (memoryCache.Cache.TryGetValue(sponsorCacheKey, out IList<Sponsor>? sponsors))
    {
        return sponsors;
    }
    sponsors = dataService.GetSponsors();
    memoryCache.Cache.Set(sponsorCacheKey, sponsors, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return sponsors;
})
.WithName("Sponsors")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

