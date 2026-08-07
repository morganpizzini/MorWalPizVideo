using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Models;
using System.Security.Claims;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class JwtRoleClaimTests
{
    [Fact]
    public void Generated_token_does_not_include_role_claim()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "this-is-a-test-secret-key-with-32-chars",
                ["JwtSettings:Issuer"] = "MorWalPizVideo.BackOffice",
                ["JwtSettings:Audience"] = "MorWalPizVideo.BackOffice",
                ["JwtSettings:ExpirationDays"] = "7"
            })
            .Build();

        var jwtService = new JwtService(configuration);
        var token = jwtService.GenerateToken(new User
        {
            Id = "user-1",
            Username = "user-1",
            Email = "user-1@example.test"
        });

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.DoesNotContain(parsed.Claims, claim => claim.Type == ClaimTypes.Role);
    }
}