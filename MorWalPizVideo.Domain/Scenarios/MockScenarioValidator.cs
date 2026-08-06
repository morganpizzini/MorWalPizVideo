using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain.Scenarios;

internal static class MockScenarioValidator
{
    public static void Validate(IMockScenario scenario)
    {
        var errors = new List<string>();
        var categories = scenario.Read<Category>("categories");
        var matches = scenario.Read<YouTubeContent>("matches");
        var categoryTitles = categories.ToDictionary(category => category.Id, category => category.Title);
        var matchIds = matches.Select(match => match.Id).ToHashSet(StringComparer.Ordinal);
        var videoIds = matches.SelectMany(match => match.VideoRefs ?? [])
            .Select(video => video.YoutubeId)
            .Where(videoId => !string.IsNullOrWhiteSpace(videoId))
            .ToHashSet(StringComparer.Ordinal);

        ValidateIds(categories, "categories", errors);
        ValidateIds(matches, "matches", errors);
        ValidateIds(scenario.Read<Product>("products"), "products", errors);
        ValidateIds(scenario.Read<ProductCategory>("productCategories"), "productCategories", errors);
        ValidateIds(scenario.Read<Sponsor>("sponsors"), "sponsors", errors);
        ValidateIds(scenario.Read<SponsorApply>("sponsorApplies"), "sponsorApplies", errors);
        ValidateIds(scenario.Read<Page>("pages"), "pages", errors);
        ValidateIds(scenario.Read<QueryLink>("queryLinks"), "queryLinks", errors);
        ValidateIds(scenario.Read<PublishSchedule>("publishSchedules"), "publishSchedules", errors);
        ValidateIds(scenario.Read<CalendarEvent>("calendarEvents"), "calendarEvents", errors);
        ValidateIds(scenario.Read<Compilation>("compilations"), "compilations", errors);
        ValidateIds(scenario.Read<BioLink>("bioLinks"), "bioLinks", errors);
        ValidateIds(scenario.Read<ShortLink>("shortLinks"), "shortLinks", errors);
        ValidateIds(scenario.Read<YTChannel>("ytchannels"), "ytchannels", errors);
        ValidateIds(scenario.Read<MorWalPizConfiguration>("configurations"), "configurations", errors);
        ValidateIds(scenario.Read<CustomForm>("customForms"), "customForms", errors);
        ValidateIds(scenario.Read<InsightTopic>("insightTopics"), "insightTopics", errors);
        ValidateIds(scenario.Read<InsightNewsItem>("insightNewsItems"), "insightNewsItems", errors);
        ValidateIds(scenario.Read<InsightContentPlan>("insightContentPlans"), "insightContentPlans", errors);
        ValidateIds(scenario.Read<InsightSourceCursor>("insightSourceCursors"), "insightSourceCursors", errors);
        ValidateIds(scenario.Read<DigitalProduct>("digitalProducts"), "digitalProducts", errors);
        ValidateIds(scenario.Read<DigitalProductCategory>("digitalProductCategories"), "digitalProductCategories", errors);
        ValidateIds(scenario.Read<Customer>("customers"), "customers", errors);
        ValidateIds(scenario.Read<Cart>("carts"), "carts", errors);
        ValidateIds(scenario.Read<Competition>("competitions"), "competitions", errors);
        ValidateIds(scenario.Read<UserChannel>("userChannels"), "userChannels", errors);
        ValidateIds(scenario.Read<UserChannelOwner>("userChannelOwners"), "userChannelOwners", errors);
        ValidateIds(scenario.Read<UserRequest>("userRequests"), "userRequests", errors);
        ValidateIds(scenario.Read<User>("users"), "users", errors);
        ValidateIds(scenario.Read<LoginAttempt>("loginAttempts"), "loginAttempts", errors);
        ValidateIds(scenario.Read<ApiKey>("apiKeys"), "apiKeys", errors);

        foreach (var match in matches)
        {
            ValidateCategories(match.Categories ?? [], $"match '{match.Id}'", categoryTitles, errors);
            foreach (var video in match.VideoRefs ?? [])
                ValidateCategories(video.Categories ?? [], $"video '{video.YoutubeId}'", categoryTitles, errors);
        }

        foreach (var calendarEvent in scenario.Read<CalendarEvent>("calendarEvents"))
        {
            if (!string.IsNullOrWhiteSpace(calendarEvent.MatchId) && !matchIds.Contains(calendarEvent.MatchId))
                errors.Add($"Calendar event '{calendarEvent.Id}' references missing match '{calendarEvent.MatchId}'.");
            ValidateCategories(calendarEvent.Categories ?? [], $"calendar event '{calendarEvent.Id}'", categoryTitles, errors);
        }

        foreach (var compilation in scenario.Read<Compilation>("compilations"))
        {
            foreach (var video in compilation.Videos.Where(video => !videoIds.Contains(video.YoutubeId)))
                errors.Add($"Compilation '{compilation.Id}' references missing video '{video.YoutubeId}'.");
        }

        var channels = scenario.Read<YTChannel>("ytchannels");
        foreach (var channel in channels)
        {
            foreach (var video in channel.Videos.Where(video => !videoIds.Contains(video.VideoId)))
                errors.Add($"Channel '{channel.ChannelId}' references missing video '{video.VideoId}'.");
        }

        var shortLinks = scenario.Read<ShortLink>("shortLinks")
            .Concat(matches.SelectMany(match => match.ShortLinks ?? []))
            .Concat(channels.SelectMany(channel => channel.ShortLinks ?? []));
        foreach (var duplicate in shortLinks.Where(link => !string.IsNullOrWhiteSpace(link.Code))
                     .GroupBy(link => link.Code, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            errors.Add($"Short-link code '{duplicate.Key}' is duplicated across scenario sources.");
        }

        var productCategoryIds = scenario.Read<ProductCategory>("productCategories").Select(item => item.Id).ToHashSet();
        foreach (var product in scenario.Read<Product>("products"))
            ValidateReferences((product.Categories ?? []).Select(category => category.Id), productCategoryIds, $"Product '{product.Id}' category", errors);

        var digitalCategoryIds = scenario.Read<DigitalProductCategory>("digitalProductCategories").Select(item => item.Id).ToHashSet();
        var digitalProducts = scenario.Read<DigitalProduct>("digitalProducts");
        foreach (var product in digitalProducts)
            ValidateReferences(product.CategoryIds ?? [], digitalCategoryIds, $"Digital product '{product.Id}' category", errors);

        var customerIds = scenario.Read<Customer>("customers").Select(item => item.Id).ToHashSet();
        var digitalProductIds = digitalProducts.Select(item => item.Id).ToHashSet();
        foreach (var cart in scenario.Read<Cart>("carts"))
        {
            ValidateReferences([cart.CustomerId], customerIds, $"Cart '{cart.Id}' customer", errors);
            ValidateReferences((cart.Items ?? []).Select(item => item.ProductId), digitalProductIds, $"Cart '{cart.Id}' product", errors);
        }

        var topicIds = scenario.Read<InsightTopic>("insightTopics").Select(item => item.Id).ToHashSet();
        var newsItems = scenario.Read<InsightNewsItem>("insightNewsItems");
        foreach (var item in newsItems)
            ValidateReferences([item.TopicId], topicIds, $"Insight news item '{item.Id}' topic", errors);
        foreach (var cursor in scenario.Read<InsightSourceCursor>("insightSourceCursors"))
            ValidateReferences([cursor.TopicId], topicIds, $"Insight cursor '{cursor.Id}' topic", errors);
        var newsItemIds = newsItems.Select(item => item.Id).ToHashSet();
        foreach (var plan in scenario.Read<InsightContentPlan>("insightContentPlans"))
        {
            ValidateReferences([plan.TopicId], topicIds, $"Insight content plan '{plan.Id}' topic", errors);
            ValidateReferences(plan.GeneratedFromNewsItemIds ?? [], newsItemIds, $"Insight content plan '{plan.Id}' news item", errors);
        }

        var queryLinkIds = scenario.Read<QueryLink>("queryLinks").Select(item => item.Id).ToHashSet();
        foreach (var schedule in scenario.Read<PublishSchedule>("publishSchedules"))
        {
            ValidateReferences([schedule.VideoId], videoIds, $"Publish schedule '{schedule.Id}' video", errors);
            ValidateReferences(schedule.QueryStringIds ?? [], queryLinkIds, $"Publish schedule '{schedule.Id}' query link", errors);
        }

        var users = scenario.Read<User>("users");
        var userIds = users.Select(item => item.Id).ToHashSet();
        var channelIds = channels.Select(item => item.ChannelId).ToHashSet();
        foreach (var subscription in scenario.Read<UserChannel>("userChannels"))
        {
            ValidateReferences([subscription.UserId], userIds, $"User channel '{subscription.Id}' user", errors);
            ValidateReferences([subscription.ChannelId], channelIds, $"User channel '{subscription.Id}' channel", errors);
        }
        foreach (var owner in scenario.Read<UserChannelOwner>("userChannelOwners"))
        {
            ValidateReferences([owner.UserId], userIds, $"Channel owner '{owner.Id}' user", errors);
            ValidateReferences([owner.ChannelId], channelIds, $"Channel owner '{owner.Id}' channel", errors);
        }
        foreach (var competition in scenario.Read<Competition>("competitions"))
        {
            if (!string.IsNullOrWhiteSpace(competition.OrganizerId))
                ValidateReferences([competition.OrganizerId], userIds, $"Competition '{competition.Id}' organizer", errors);
            foreach (var evaluation in (competition.Stages ?? []).SelectMany(stage => stage.Evaluations ?? []))
                ValidateReferences([evaluation.UserId], userIds, $"Competition '{competition.Id}' evaluation user", errors);
        }

        var administrators = users
            .Where(user => string.Equals(user.Username, "MorWalPiz", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var adminHasBackofficePermission = administrators.Count == 1 &&
            (administrators[0].CanAccessBackoffice ||
             (administrators[0].DirectPermissions ?? [])
                .Any(permission => string.Equals(permission, AuthorizationPermissionKeys.CanAccessBackoffice, StringComparison.OrdinalIgnoreCase)));

        if (administrators.Count != 1 || !administrators[0].IsActive || !adminHasBackofficePermission ||
            !string.Equals(administrators[0].Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The scenario must contain one active MorWalPiz BackOffice administrator.");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException($"Invalid mock scenario:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static void ValidateIds<T>(IList<T> items, string collectionName, ICollection<string> errors)
        where T : BaseEntity
    {
        if (items.Any(item => string.IsNullOrWhiteSpace(item.Id)))
            errors.Add($"Collection '{collectionName}' contains an item without an ID.");

        foreach (var duplicate in items.Where(item => !string.IsNullOrWhiteSpace(item.Id)).GroupBy(item => item.Id).Where(group => group.Count() > 1))
            errors.Add($"Collection '{collectionName}' contains duplicate ID '{duplicate.Key}'.");
    }

    private static void ValidateCategories(
        IEnumerable<CategoryRef> references,
        string owner,
        IReadOnlyDictionary<string, string> categoryTitles,
        ICollection<string> errors)
    {
        foreach (var reference in references)
        {
            if (!categoryTitles.TryGetValue(reference.Id, out var title))
                errors.Add($"{owner} references missing category '{reference.Id}'.");
            else if (!string.Equals(title, reference.Title, StringComparison.Ordinal))
                errors.Add($"{owner} has stale title for category '{reference.Id}'.");
        }
    }

    private static void ValidateReferences(
        IEnumerable<string> references,
        IReadOnlySet<string> validIds,
        string relationship,
        ICollection<string> errors)
    {
        foreach (var reference in references.Where(reference => !string.IsNullOrWhiteSpace(reference)))
        {
            if (!validIds.Contains(reference))
                errors.Add($"{relationship} references missing ID '{reference}'.");
        }
    }
}