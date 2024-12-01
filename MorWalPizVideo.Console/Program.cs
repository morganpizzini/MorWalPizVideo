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
    Console.WriteLine("1 - add element");
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
            Console.WriteLine("Is link? (Y/n)");
            var isLinkResponse = Console.ReadLine();
            var isLink = isLinkResponse?.ToLower() == "n" ? false : true;

            matchCollection.InsertOne(new Match(element,isLink,category));
            var json = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/reset");
            break;
        default:
            Console.WriteLine("Invalid choice");
            break;
    }
    Console.WriteLine("");
    Console.WriteLine("---");
    Console.WriteLine("");
}


