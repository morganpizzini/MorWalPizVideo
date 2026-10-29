using MongoDB.Driver;

namespace MorWalPizVideo.Domain.FreezerAnalysis;

public sealed class FreezerAnalysisDatabase(IMongoDatabase database)
{
    public IMongoDatabase Database { get; } = database;
}
