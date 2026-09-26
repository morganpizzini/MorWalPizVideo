using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<ScriptStudioQuotaConsumption> ConsumeScriptStudioQuotaAsync(string userId, string period, CancellationToken cancellationToken = default);
}

public sealed record ScriptStudioQuotaConsumption(bool Consumed, int Limit, int UsedBefore, int UsedAfter, string Period);
