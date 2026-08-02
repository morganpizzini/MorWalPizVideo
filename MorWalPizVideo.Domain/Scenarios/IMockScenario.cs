using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain.Scenarios;

public interface IMockScenario
{
    void Reset();
    IList<T> Read<T>(string collectionName) where T : BaseEntity;
    T Add<T>(string collectionName, T item) where T : BaseEntity;
    void Replace<T>(string collectionName, T item) where T : BaseEntity;
    void Delete<T>(string collectionName, string id) where T : BaseEntity;
}