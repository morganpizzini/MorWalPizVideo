using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
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

apiGroup.MapGet("/reset", (DataService dataService) =>
{
    dataService.ResetItems();
    return Results.Ok();
})
.WithName("Reset")
.WithOpenApi();

apiGroup.MapGet("/matches", async (DataService dataService, MyMemoryCache memoryCache, IHttpClientFactory httpClientFactory) =>
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
    query["key"] = "";
    string queryString = query.ToString() ?? "";

    var httpResponseMessage = await httpClient.GetAsync($"?{queryString}");
    if (httpResponseMessage.IsSuccessStatusCode)
    {
        using var contentStream =
            await httpResponseMessage.Content.ReadAsStreamAsync();

        //GitHubBranches = await JsonSerializer.DeserializeAsync
        //    <IEnumerable<GitHubBranch>>(contentStream);
        memoryCache.Cache.Set(cacheKey, matches, new MemoryCacheEntryOptions
        {
            Size = 1
        });
    }
    return matches;
})
.WithName("Matches")
.WithOpenApi();

apiGroup.MapGet("/matches/{url}", (DataService dataService, string url) =>
{
    return dataService.GetItems().FirstOrDefault(x => x.Url == url);
})
.WithName("Match Detail")
.WithOpenApi();

apiGroup.MapGet("/products", (DataService dataService) =>
{
    return dataService.GetProducts();
})
.WithName("Products")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

