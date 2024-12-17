using MongoDB.Driver;
using System.Net.Http.Headers;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();

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

var siteUrl = builder.Configuration["SiteUrl"];

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

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MorWalPiz backoffice API"));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
