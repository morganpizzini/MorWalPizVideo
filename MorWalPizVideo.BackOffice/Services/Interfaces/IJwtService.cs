using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    string? ValidateToken(string token);
}
