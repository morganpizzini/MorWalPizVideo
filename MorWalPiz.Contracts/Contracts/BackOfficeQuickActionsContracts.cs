namespace MorWalPiz.Contracts.Contracts;

public sealed record QuickActionEntry(
    string Id,
    string Kind,
    string? ActionKey = null,
    string? Label = null,
    string? Url = null);

public sealed record SaveQuickActionsRequest(IReadOnlyList<QuickActionEntry> Actions);

public sealed record QuickActionDefinition(string Key, string Label, string Url, string Icon);

public sealed record ResolvedQuickAction(string Id, string Kind, string Label, string Url, string Icon);

public sealed record QuickActionsResponse(
    bool IsConfigured,
    string ChannelId,
    DateTime? UpdatedAtUtc,
    bool CanEdit,
    IReadOnlyList<QuickActionEntry> Actions,
    IReadOnlyList<QuickActionDefinition> AvailableActions,
    IReadOnlyList<ResolvedQuickAction> VisibleActions);
