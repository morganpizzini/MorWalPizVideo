const storagePrefix = "workshop.presentation.v1";

export type WorkshopOnboardingState = "active" | "skipped" | "completed";
export type WorkshopGoalKind = "shootingRounds" | "producedRounds";
export interface WorkshopPersonalGoal {
  kind: WorkshopGoalKind;
  target: number;
}

export interface WorkshopPresentationPreferences {
  dashboardDays: 30 | 90 | 365;
  lastRecipeByFirearm: Record<string, string>;
  historyOperationType: string;
  personalGoal: WorkshopPersonalGoal | null;
  onboardingState: WorkshopOnboardingState;
  onboardingStep: 0 | 1 | 2 | 3 | 4;
}

const defaults: WorkshopPresentationPreferences = {
  dashboardDays: 30,
  lastRecipeByFirearm: {},
  historyOperationType: "",
  personalGoal: null,
  onboardingState: "active",
  onboardingStep: 0,
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
    const historyOperationType = typeof value.historyOperationType === "string" && value.historyOperationType.length <= 64
      ? value.historyOperationType
      : "";
    const personalGoal = value.personalGoal && typeof value.personalGoal === "object"
      && (value.personalGoal.kind === "shootingRounds" || value.personalGoal.kind === "producedRounds")
      && typeof value.personalGoal.target === "number" && Number.isSafeInteger(value.personalGoal.target)
      && value.personalGoal.target > 0 && value.personalGoal.target <= 1_000_000_000
      ? value.personalGoal
      : null;
    const onboardingState = value.onboardingState === "skipped" || value.onboardingState === "completed"
      ? value.onboardingState
      : "active";
    const onboardingStep = value.onboardingStep === 1 || value.onboardingStep === 2 || value.onboardingStep === 3 || value.onboardingStep === 4
      ? value.onboardingStep
      : 0;
    return { dashboardDays, lastRecipeByFirearm, historyOperationType, personalGoal, onboardingState, onboardingStep };
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
