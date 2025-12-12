using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> AuthenticateAsync(string username, string password);
}
