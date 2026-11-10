using System.Security.Claims;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class QuickActionsValidationException(string message) : Exception(message);

public sealed class DashboardQuickActionsService(
    IBackOfficeQuickActionPreferencesRepository repository,
    IUserAuthorizationEvaluator authorization)
{
    private sealed record CatalogEntry(QuickActionDefinition Definition, string[] Permissions);

    private static readonly CatalogEntry[] Catalog =
    [
        new(new("videos.manage", "Gestisci video", "/videos", "videos"), [AuthorizationPermissionKeys.VideosView, AuthorizationPermissionKeys.VideosManage]),
        new(new("articles.create", "Scrivi articolo", "/blogposts/create", "article-create"), [AuthorizationPermissionKeys.PagesCreate, AuthorizationPermissionKeys.PagesManage]),
        new(new("articles.list", "Tutti gli articoli", "/blogposts", "articles"), [AuthorizationPermissionKeys.PagesView, AuthorizationPermissionKeys.PagesManage]),
        new(new("videos.import", "Importa video", "/videos/import", "videos"), [AuthorizationPermissionKeys.VideosImport, AuthorizationPermissionKeys.VideosManage]),
        new(new("calendar.open", "Calendario", "/calendarevents", "calendar"), [AuthorizationPermissionKeys.CalendarView, AuthorizationPermissionKeys.CalendarManage]),
        new(new("editorial.open", "Produzione editoriale", "/editorialproductioncards", "editorial"), [AuthorizationPermissionKeys.EditorialProductionCardsView, AuthorizationPermissionKeys.EditorialProductionCardsManage]),
        new(new("shortlinks.open", "Short link", "/shortlinks", "link"), [AuthorizationPermissionKeys.ShortLinksView, AuthorizationPermissionKeys.ShortLinksManage]),
        new(new("scripts.open", "Script Studio", "/script-studio", "editorial"), [AuthorizationPermissionKeys.ScriptsStudio])
    ];

    public async Task<QuickActionsResponse> GetAsync(ClaimsPrincipal user, string channelId, CancellationToken cancellationToken)
    {
        var stored = await repository.GetAsync(EffectiveId(user), channelId, cancellationToken);
        var available = await GetAvailableAsync(user);
        var actions = stored is null
            ? Catalog.Take(3).Where(item => available.Any(value => value.Key == item.Definition.Key))
                .Select(item => new QuickActionEntry($"default-{item.Definition.Key}", "predefined", item.Definition.Key)).ToArray()
            : stored.Actions.Select(ToContract).ToArray();
        var definitions = available.ToDictionary(item => item.Key);
        var visible = new List<ResolvedQuickAction>();
        foreach (var action in actions)
        {
            if (action.Kind == "custom")
                visible.Add(new(action.Id, action.Kind, action.Label!, action.Url!, "link"));
            else if (action.ActionKey is not null && definitions.TryGetValue(action.ActionKey, out var definition))
                visible.Add(new(action.Id, action.Kind, definition.Label, definition.Url, definition.Icon));
        }
        return new(stored is not null, channelId, stored?.UpdatedAtUtc,
            !user.HasClaim("impersonation", "true"), actions, available, visible);
    }

    public async Task<QuickActionsResponse> SaveAsync(ClaimsPrincipal user, string channelId,
        SaveQuickActionsRequest request, CancellationToken cancellationToken)
    {
        if (request.Actions is null)
            throw new QuickActionsValidationException("Actions must be an array.");
        var current = await GetAsync(user, channelId, cancellationToken);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in request.Actions)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id))
                throw new QuickActionsValidationException("Each action must have a unique nonempty ID.");
            if (entry.Kind == "custom")
            {
                if (entry.Label is null || entry.Url is null)
                    throw new QuickActionsValidationException("Custom labels and URLs must be strings.");
            }
            else if (entry.Kind == "predefined")
            {
                var available = current.AvailableActions.Any(item => item.Key == entry.ActionKey);
                var retained = current.Actions.Any(item =>
                    item.Id == entry.Id && item.Kind == "predefined" && item.ActionKey == entry.ActionKey);
                if (entry.ActionKey is null || (!available && !retained))
                    throw new QuickActionsValidationException("The selected predefined action is unavailable.");
            }
            else
                throw new QuickActionsValidationException("Unknown action kind.");
        }
        await repository.UpsertAsync(new BackOfficeQuickActionPreferences
        {
            UserId = EffectiveId(user),
            ChannelId = channelId,
            CreationDateTime = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Actions = request.Actions.Select(item => new BackOfficeQuickActionEntry
            {
                Id = item.Id,
                Kind = item.Kind,
                ActionKey = item.Kind == "predefined" ? item.ActionKey : null,
                Label = item.Kind == "custom" ? item.Label : null,
                Url = item.Kind == "custom" ? item.Url : null
            }).ToList()
        }, cancellationToken);
        return await GetAsync(user, channelId, cancellationToken);
    }

    public async Task<QuickActionsResponse> ResetAsync(ClaimsPrincipal user, string channelId, CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(EffectiveId(user), channelId, cancellationToken);
        return await GetAsync(user, channelId, cancellationToken);
    }

    private async Task<IReadOnlyList<QuickActionDefinition>> GetAvailableAsync(ClaimsPrincipal user)
    {
        var available = new List<QuickActionDefinition>();
        foreach (var item in Catalog)
        {
            var requirements = item.Permissions.Select(permission =>
                new AllowUserToken(AllowUserRequirementType.Permission, permission)).ToArray();
            if (await authorization.IsAuthorizedAsync(user, requirements))
                available.Add(item.Definition);
        }
        return available;
    }

    private static string EffectiveId(ClaimsPrincipal user) => ImpersonationClaimsTransformation.GetEffectiveUserId(user);
    private static QuickActionEntry ToContract(BackOfficeQuickActionEntry item) =>
        new(item.Id, item.Kind, item.ActionKey, item.Label, item.Url);
}
