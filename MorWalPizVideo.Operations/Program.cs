using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Operations;
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

var matchCollection = database.GetCollection<Match>(DbCollections.Matches);
var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);
var calendarEventsCollection = database.GetCollection<CalendarEvent>(DbCollections.CalendarEvents);
var bioLinksCollection = database.GetCollection<BioLink>(DbCollections.BioLinks);

var siteUrl = config["SiteUrl"];
if (string.IsNullOrEmpty(siteUrl))
{
    throw new NullReferenceException("config SiteUrl is empty");
}

using HttpClient client = new();
client.BaseAddress = new Uri($"{siteUrl}api/");
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));

while (true)
{
    Console.WriteLine("0 - exit");
    Console.WriteLine("1 - add single link video");
    Console.WriteLine("2 - add root element");
    Console.WriteLine("3 - add subVideo element");
    Console.WriteLine("4 - add calendar event");
    Console.WriteLine("5 - update calendar event");
    Console.WriteLine("6 - create video shortlink");
    Console.WriteLine("7 - get video shortlink");
    Console.WriteLine("8 - create bio link");
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
            await VideoWorkflow.SingleVideo(matchCollection, client);
            break;
        case "2":
            VideoWorkflow.RootElement(matchCollection);
            break;
        case "3":
            await VideoWorkflow.SubVideo(matchCollection, client);
            break;
        case "4":
            await AppWorkflow.AddCalendarEvent(calendarEventsCollection, client);
            break;
        case "5":
            await AppWorkflow.UpdateCalendarEvent(calendarEventsCollection, client);
            break;
        case "6":
            await VideoWorkflow.CreateVideoShortlink(matchCollection, shortLinkCollection, client, siteUrl);
            break;
        case "7":
            await VideoWorkflow.GetVideoShortlink(shortLinkCollection, siteUrl);
            break;
        case "8":
            await BioWorkflow.CreateBioLink(bioLinksCollection, client);
            break;
        case "9":
            await BioWorkflow.UpdateBioLink(bioLinksCollection, client);
            break;
        case "10":
            await BioWorkflow.ToggleBioLink(bioLinksCollection, client);
            break;
        case "11":
            await BioWorkflow.DeleteBioLink(bioLinksCollection, client);
            break;
        default:
            Console.WriteLine("Invalid choice");
            break;
    }
    Console.WriteLine("");
    Console.WriteLine("---");
    Console.WriteLine("");
}


