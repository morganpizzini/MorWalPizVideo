using MongoDB.Driver;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;
using System.Net.Http.Headers;
using System.Security.Authentication;

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

if (string.IsNullOrEmpty(siteUrl))
{
    throw new NullReferenceException("config SiteUrl is empty");
}

builder.Services.AddHttpClient("MorWalPiz", httpClient =>
{
    httpClient.BaseAddress = new Uri(siteUrl);
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

// Aggiungi HttpClient
builder.Services.AddHttpClient("Discord", client =>
{
    client.BaseAddress = new Uri("https://discord.com/api/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bot", discordSettings.Token);
});

builder.Services.AddHttpClient("Telegram", httpClient =>
{
    httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{telegramSettings.Token}/sendMessage");
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});


if(builder.Environment.IsDevelopment())
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

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MorWalPiz backoffice API"));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
