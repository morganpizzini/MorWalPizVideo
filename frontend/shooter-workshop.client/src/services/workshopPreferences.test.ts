import { beforeEach, describe, expect, it } from "vitest";
import {
  readWorkshopPreferences,
  writeWorkshopPreferences,
} from "./workshopPreferences";

describe("Workshop presentation preferences", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("defaults onboarding to an active first step", () => {
    expect(readWorkshopPreferences("account-1")).toMatchObject({
      onboardingState: "active",
      onboardingStep: 0,
    });
  });

  it("rejects invalid onboarding values without affecting another account", () => {
    localStorage.setItem("workshop.presentation.v1:account-1", JSON.stringify({
      onboardingState: "unexpected",
      onboardingStep: 99,
    }));

    expect(readWorkshopPreferences("account-1")).toMatchObject({
      onboardingState: "active",
      onboardingStep: 0,
    });

    writeWorkshopPreferences("account-2", {
      ...readWorkshopPreferences("account-2"),
      onboardingState: "completed",
      onboardingStep: 4,
    });
    expect(readWorkshopPreferences("account-1").onboardingState).toBe("active");
    expect(readWorkshopPreferences("account-2").onboardingState).toBe("completed");
  });

  it("keeps validated account-local goals", () => {
    localStorage.setItem("workshop.presentation.v1:account-1", JSON.stringify({
      personalGoal: { kind: "shootingRounds", target: 500 },
    }));
    localStorage.setItem("workshop.presentation.v1:account-2", JSON.stringify({
      personalGoal: { kind: "shootingRounds", target: -1 },
    }));

    expect(readWorkshopPreferences("account-1").personalGoal).toEqual({
      kind: "shootingRounds",
      target: 500,
    });
    expect(readWorkshopPreferences("account-2").personalGoal).toBeNull();
  });
});
