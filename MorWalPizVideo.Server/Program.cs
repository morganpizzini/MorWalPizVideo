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

apiGroup.MapGet("/matches", async (IConfiguration configuration, DataService dataService, MyMemoryCache memoryCache, IHttpClientFactory httpClientFactory) =>
{
    var cacheKey = "data";
    if(memoryCache.Cache.TryGetValue(cacheKey,out IList<Match>? matches))
    {
        return matches;
    }
    matches = dataService.GetItems();

    var httpClient = httpClientFactory.CreateClient("Youtube");

    var query = HttpUtility.ParseQueryString(string.Empty);

    query["part"] = "id,snippet,statistics,contentDetails";
    query["id"] = string.Join(",", matches.Where(x => x.Videos != null).SelectMany(x => x.Videos).Select(x => x.Id).ToList().Concat(matches.Select(m=>m.ThumbnailUrl).ToList()));
    query["key"] = configuration["YTApiKey"];
    string queryString = query.ToString() ?? "";

    var httpResponseMessage = await httpClient.GetAsync($"?{queryString}");
    if (!httpResponseMessage.IsSuccessStatusCode)
        return matches;
    
    using var contentStream =
        await httpResponseMessage.Content.ReadAsStreamAsync();

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    var youtubeResponse = await JsonSerializer.DeserializeAsync<VideoResponse>(contentStream,options);

    if(youtubeResponse == null)
        return matches;

    foreach (var item in youtubeResponse.Items)
    {
        var video = ContractUtils.Convert(item);
        
        var match = matches.FirstOrDefault(x => x.Videos != null && x.Videos.Any(v => v.Id == video.Id));
        if (match != null)
        {
            // is related video
            var index = Array.FindIndex(match.Videos, x => x.Id == video.Id);
            match.Videos[index] = video with { Category = match.Videos[index].Category };
        } 
        else
        {
            //is home page video
            var element = matches.FirstOrDefault(x => x.ThumbnailUrl == video.Id);
            if(element == null)
                continue;
            var index = matches.IndexOf(element);

            matches[index] = element with { Title = video.Title, Description = video.Description, CreationDateTime = video.CreationDateTime, Url = video.Id };
        }
    }
    
    memoryCache.Cache.Set(cacheKey, matches.OrderByDescending(x=>x.CreationDateTime), new MemoryCacheEntryOptions
    {
        Size = 1
    });
    
    return matches;
})
.WithName("Matches")
.WithOpenApi();

apiGroup.MapGet("/matches/{url}", (DataService dataService, MyMemoryCache memoryCache, string url) =>
{
    var cacheKey = "data";
    if (!memoryCache.Cache.TryGetValue(cacheKey, out IList<Match>? matches))
    {
        matches = dataService.GetItems();
    }
    return matches?.FirstOrDefault(x => x.Url == url);
})
.WithName("Match Detail")
.WithOpenApi();

apiGroup.MapGet("/products", (DataService dataService, MyMemoryCache memoryCache) =>
{
    var cacheKey = "products";
    if (memoryCache.Cache.TryGetValue(cacheKey, out IList<Product>? products))
    {
        return products;
    }
    products = dataService.GetProducts();
    memoryCache.Cache.Set(cacheKey, products, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return products;
})
.WithName("Products")
.WithOpenApi();

apiGroup.MapGet("/sponsors", (DataService dataService, MyMemoryCache memoryCache) =>
{
    var cacheKey = "sponsors";
    if (memoryCache.Cache.TryGetValue(cacheKey, out IList<Sponsor>? sponsors))
    {
        return sponsors;
    }
    sponsors = dataService.GetSponsors();
    memoryCache.Cache.Set(cacheKey, sponsors, new MemoryCacheEntryOptions
    {
        Size = 1
    });
    return sponsors;
})
.WithName("Sponsors")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

