using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MorWalPizVideo.Console;
using MorWalPizVideo.Server.Models;
using System.Net.Http.Headers;
using System.Security.Authentication;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// Get values from the config given their key and their target type.
MorWalPizDatabaseSettings? dbConfig = config.GetSection("MorWalPizDatabase").Get<MorWalPizDatabaseSettings>();
if(dbConfig == null)
{
    Console.WriteLine("Cannot read configuration");
    return;
}

MongoClientSettings settings = MongoClientSettings.FromUrl(
    new MongoUrl(dbConfig.ConnectionString)
);
settings.SslSettings =
new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

var database = new MongoClient(settings).GetDatabase(dbConfig.DatabaseName);

var matchCollection = database.GetCollection<Match>("matches");


using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));


while (true)
{
    Console.WriteLine("0 - exit");
    Console.WriteLine("1 - add single link video");
    Console.WriteLine("2 - add root element");
    Console.WriteLine("make a chioce");
    Console.WriteLine("");
    var choice = Console.ReadLine();
    switch(choice)
    {
        case "0":
            Console.WriteLine("exit - press a key");
            Console.ReadKey();
            return;
        case "1":
            Console.WriteLine("Enter video ID");
            var element = Console.ReadLine();
            if (string.IsNullOrEmpty(element))
            {
                Console.WriteLine("Not a valid ID");
                continue;
            }
            Console.WriteLine("Enter Category");
            var category = Console.ReadLine();
            if (string.IsNullOrEmpty(category))
            {
                Console.WriteLine("Not a valid category");
                continue;
            }
            matchCollection.InsertOne(new Match(element,true,category));
            var json = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/reset");
            json = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/matches");
            break;
        case "2":
            Console.WriteLine("Enter root ID");
            var element2 = Console.ReadLine();
            if (string.IsNullOrEmpty(element2))
            {
                Console.WriteLine("Not a valid ID");
                continue;
            }
            Console.WriteLine("Enter Category");
            var category2 = Console.ReadLine();
            if (string.IsNullOrEmpty(category2))
            {
                Console.WriteLine("Not a valid category");
                continue;
            }
            Console.WriteLine("Enter title");
            var title = Console.ReadLine();
            if (string.IsNullOrEmpty(title))
            {
                Console.WriteLine("Not a valid title");
                continue;
            }
            Console.WriteLine("Enter description");
            var description = Console.ReadLine();
            if (string.IsNullOrEmpty(description))
            {
                Console.WriteLine("Not a valid description");
                continue;
            }
            var url = Console.ReadLine();
            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("Not a valid url");
                continue;
            }
            matchCollection.InsertOne(new Match(element2, title, description, url, [], category2));
            break;
        case "3":
            Console.WriteLine("Enter video ID");
            var element3 = Console.ReadLine();
            if (string.IsNullOrEmpty(element3))
            {
                Console.WriteLine("Not a valid ID");
                continue;
            }
            var existingMatch = matchCollection.Find(x => x.ThumbnailUrl == element3).FirstOrDefault();
            if (existingMatch == null)
            {
                Console.WriteLine("Match do not exists");
                continue;
            }

            Console.WriteLine("Enter video ID");
            var videoId = Console.ReadLine();
            if (string.IsNullOrEmpty(videoId))
            {
                Console.WriteLine("Not a valid ID");
                continue;
            }
            Console.WriteLine("Enter Category");
            var category3 = Console.ReadLine();
            if (string.IsNullOrEmpty(category3))
            {
                Console.WriteLine("Not a valid category");
                continue;
            }
            existingMatch = existingMatch with { Videos =  [.. existingMatch.Videos, new Video(videoId, category3)] };

            await matchCollection.ReplaceOneAsync(Builders<Match>.Filter.Eq(e => e.Id, existingMatch.Id), existingMatch);

            var json3 = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/reset");
            json3 = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/matches");
            break;
        default:
            Console.WriteLine("Invalid choice");
            break;
    }
    Console.WriteLine("");
    Console.WriteLine("---");
    Console.WriteLine("");
}


