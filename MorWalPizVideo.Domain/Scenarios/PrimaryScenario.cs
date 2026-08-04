using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.Security.Cryptography;

namespace MorWalPizVideo.Domain.Scenarios;

public class PrimaryScenario : BaseScenario
{
    public const string AdminUsername = "MorWalPiz";
    public const string AdminEmail = "morwalpiz@example.test";
    public const string AdminPassword = "MockPassword123!";
    private const string AdminPasswordSalt = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";
    public const string AdminUserId = "100000000000000000000001";
    public const string MatchId = "200000000000000000000001";
    public const string VideoId = "scenario-video-1";
    public const string ChannelId = ContentConstants.MorWalPizYouTubeChannelId;
    public const string StandaloneShortLinkCode = "test1";
    public const string MatchShortLinkCode = "match1";
    public const string ChannelShortLinkCode = "channel1";

    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void Initialize()
    {
        var category = new Category("Scenario category", "Canonical mock category")
        {
            Id = "300000000000000000000001",
            CreationDateTime = CreatedAt
        };
        var categoryReference = new CategoryRef(category.Id, category.Title);
        var video = new VideoRef(VideoId, [categoryReference], "Scenario video", "Canonical mock video", channelIds: [ChannelId])
        {
            CreationDateTime = CreatedAt
        };
        var matchShortLink = CreateShortLink(
            "400000000000000000000002",
            MatchShortLinkCode,
            MatchId,
            LinkType.YouTubeVideo);
        var channelShortLink = CreateShortLink(
            "400000000000000000000003",
            ChannelShortLinkCode,
            ChannelId,
            LinkType.YouTubeChannel);

        Set("categories", [category]);
        Set("matches",
        [
            new YouTubeContent(
                "scenario-content-1",
                "Scenario match",
                "Canonical mock match",
                "scenario-match",
                VideoId,
                [video],
                [categoryReference],
                YoutubeContentType.Collection)
            {
                Id = MatchId,
                CreationDateTime = CreatedAt,
                CreatorUserId = "test-user-id",
                ShortLinks = [matchShortLink]
            }
        ]);
        Set("ytchannels",
        [
            new YTChannel(ChannelId, "Scenario channel")
            {
                Id = "500000000000000000000001",
                Mine = true,
                CreationDateTime = CreatedAt,
                Videos = [new YouTubeVideo { VideoId = VideoId, Title = video.Title }],
                ShortLinks = [channelShortLink]
            }
        ]);
        Set("shortLinks",
        [
            CreateShortLink(
                "400000000000000000000001",
                StandaloneShortLinkCode,
                "https://example.test/scenario",
                LinkType.CustomUrl)
        ]);

        var passwordHash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            AdminPassword,
            Convert.FromBase64String(AdminPasswordSalt),
            100000,
            HashAlgorithmName.SHA256,
            256));
        Set("users",
        [
            new User
            {
                Id = AdminUserId,
                CreationDateTime = CreatedAt,
                Username = AdminUsername,
                Email = AdminEmail,
                PasswordHash = passwordHash,
                Salt = AdminPasswordSalt,
                IsActive = true,
                Role = "admin",
                CanAccessBackoffice = true
            },
            new User
            {
                Id = "test-user-id",
                CreationDateTime = CreatedAt,
                Username = "test-user",
                Email = "test-user@example.test",
                IsActive = true,
                Role = "user",
                CanAccessBackoffice = true
            }
        ]);

        Set("products", Array.Empty<Product>());
        Set("productCategories", Array.Empty<ProductCategory>());
        Set("sponsors", Array.Empty<Sponsor>());
        Set("sponsorApplies", Array.Empty<SponsorApply>());
        Set("pages", Array.Empty<Page>());
        Set("queryLinks", Array.Empty<QueryLink>());
        Set("publishSchedules", Array.Empty<PublishSchedule>());
        Set("calendarEvents", Array.Empty<CalendarEvent>());
        Set("compilations", Array.Empty<Compilation>());
        Set("bioLinks", Array.Empty<BioLink>());
        Set("configurations", Array.Empty<MorWalPizConfiguration>());
        Set("customForms", Array.Empty<CustomForm>());
        Set("customFormResponses", Array.Empty<CustomFormResponseDocument>());
        Set("insightTopics", Array.Empty<InsightTopic>());
        Set("insightNewsItems", Array.Empty<InsightNewsItem>());
        Set("insightContentPlans", Array.Empty<InsightContentPlan>());
        Set("insightSourceCursors", Array.Empty<InsightSourceCursor>());
        Set("digitalProducts", Array.Empty<DigitalProduct>());
        Set("digitalProductCategories", Array.Empty<DigitalProductCategory>());
        Set("customers", Array.Empty<Customer>());
        Set("carts", Array.Empty<Cart>());
        Set("competitions", Array.Empty<Competition>());
        Set("userChannels", Array.Empty<UserChannel>());
        Set("userChannelOwners", [new UserChannelOwner
        {
            Id = "600000000000000000000001",
            UserId = "test-user-id",
            ChannelId = ChannelId,
            IsActive = true
        }]);
        Set("userRequests", Array.Empty<UserRequest>());
        Set("loginAttempts", Array.Empty<LoginAttempt>());
        Set("apiKeys", Array.Empty<ApiKey>());
    }

    private static ShortLink CreateShortLink(string id, string code, string target, LinkType linkType) =>
        new(code, target, Array.Empty<QueryLink>())
        {
            Id = id,
            CreationDateTime = CreatedAt,
            LinkType = linkType
        };
}
