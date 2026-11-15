const storagePrefix = "workshop.presentation.v1";

export interface WorkshopPresentationPreferences {
  dashboardDays: 30 | 90 | 365;
  lastRecipeByFirearm: Record<string, string>;
}

const defaults: WorkshopPresentationPreferences = {
  dashboardDays: 30,
  lastRecipeByFirearm: {},
};

function key(accountId: string) {
  return `${storagePrefix}:${accountId}`;
}

export function readWorkshopPreferences(accountId: string): WorkshopPresentationPreferences {
  if (!accountId || typeof localStorage === "undefined") return defaults;
  try {
    const raw = localStorage.getItem(key(accountId));
    if (!raw) return defaults;
    const parsed: unknown = JSON.parse(raw);
    if (!parsed || typeof parsed !== "object") return defaults;
    const value = parsed as Partial<WorkshopPresentationPreferences>;
    const dashboardDays = value.dashboardDays === 90 || value.dashboardDays === 365 ? value.dashboardDays : 30;
    const lastRecipeByFirearm = value.lastRecipeByFirearm && typeof value.lastRecipeByFirearm === "object"
      ? Object.fromEntries(Object.entries(value.lastRecipeByFirearm).filter(([, recipeId]) => typeof recipeId === "string"))
      : {};
    return { dashboardDays, lastRecipeByFirearm };
  } catch (error) {
    console.warn("Workshop presentation preferences could not be read.", error);
    return defaults;
  }
}

export function writeWorkshopPreferences(accountId: string, preferences: WorkshopPresentationPreferences) {
  if (!accountId || typeof localStorage === "undefined") return;
  try {
    localStorage.setItem(key(accountId), JSON.stringify(preferences));
  } catch (error) {
    console.warn("Workshop presentation preferences could not be saved.", error);
  }
}
