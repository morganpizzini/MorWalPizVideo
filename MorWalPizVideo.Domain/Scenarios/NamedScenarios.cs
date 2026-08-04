using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain.Scenarios;

public sealed class EmptyScenario : PrimaryScenario
{
    protected override void Initialize()
    {
        base.Initialize();
        Clear<YouTubeContent>("matches");
        Clear<YTChannel>("ytchannels");
        Clear<ShortLink>("shortLinks");
        Clear<Category>("categories");
        Clear<Product>("products");
        Clear<Page>("pages");
        Clear<CalendarEvent>("calendarEvents");
        Clear<Compilation>("compilations");
    }
}

public sealed class AuthorizationScenario : PrimaryScenario
{
    protected override void Initialize()
    {
        base.Initialize();
        Set("users", Array.Empty<User>().Concat(new[]
        {
            new User
            {
                Id = "100000000000000000000002",
                CreationDateTime = new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc),
                Username = "inactive-user",
                Email = "inactive-user@example.test",
                IsActive = false,
                Role = "user"
            }
        }).Concat(Read<User>("users")));
    }
}

public sealed class ExternalFailureScenario : PrimaryScenario;

public sealed class LegacyCompatibilityScenario : PrimaryScenario;