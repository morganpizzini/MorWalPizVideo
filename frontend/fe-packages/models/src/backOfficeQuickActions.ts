export type QuickActionEntry =
  | { id: string; kind: "predefined"; actionKey: string }
  | { id: string; kind: "custom"; label: string; url: string };

export interface QuickActionDefinition {
  key: string;
  label: string;
  url: string;
  icon: string;
}

export interface ResolvedQuickAction {
  id: string;
  kind: "predefined" | "custom";
  label: string;
  url: string;
  icon: string;
}

export interface QuickActionsResponse {
  isConfigured: boolean;
  channelId: string;
  updatedAtUtc: string | null;
  canEdit: boolean;
  actions: QuickActionEntry[];
  availableActions: QuickActionDefinition[];
  visibleActions: ResolvedQuickAction[];
}

export interface SaveQuickActionsRequest {
  actions: QuickActionEntry[];
}
