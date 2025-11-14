using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MorWalPizVideo.Server.Utils;
using System.Security.Authentication;

namespace MorWalPizVideo.Server.Services
{
    public interface IMongoDbService
    {
        IMongoDatabase GetDatabase();
    }

    public class MongoDbService : IMongoDbService
    {
        private readonly Lazy<IMongoDatabase> _database;

        public MongoDbService(IOptions<MorWalPizDatabaseSettings> options)
        {
            _database = new Lazy<IMongoDatabase>(() =>
            {
                var settings = options.Value;

                if (string.IsNullOrEmpty(settings.ConnectionString) || string.IsNullOrEmpty(settings.DatabaseName))
                {
                    throw new InvalidOperationException("MongoDB configuration is not properly set. Check your configuration sources including Azure Key Vault.");
                }

                var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(settings.ConnectionString));
                mongoSettings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

                var client = new MongoClient(mongoSettings);
                return client.GetDatabase(settings.DatabaseName);

            });
        }

        public IMongoDatabase GetDatabase() => _database.Value;
    }
}
