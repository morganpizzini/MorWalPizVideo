import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "../../i18n";
import { readWorkshopPreferences } from "../../services/workshopPreferences";
import OnboardingGuide from "./OnboardingGuide";

describe("Workshop onboarding guide", () => {
  afterEach(cleanup);

  beforeEach(() => {
    localStorage.clear();
  });

  function renderGuide(onDismiss = vi.fn()) {
    return {
      onDismiss,
      ...render(
        <I18nProvider>
          <MemoryRouter>
            <OnboardingGuide accountId="account-1" onDismiss={onDismiss} />
          </MemoryRouter>
        </I18nProvider>,
      ),
    };
  }

  it("uses canonical route links and records presentation-only progress", () => {
    renderGuide();

    expect(screen.getByRole("link", { name: "Open firearms" }).getAttribute("href")).toBe("/firearms");
    expect(screen.getByRole("link", { name: "Record production" }).getAttribute("href")).toBe("/ammunition");
    fireEvent.click(screen.getByRole("link", { name: "Open firearms" }));

    expect(readWorkshopPreferences("account-1").onboardingStep).toBe(1);
    expect(readWorkshopPreferences("account-2").onboardingStep).toBe(0);
  });

  it("allows a user to skip or finish without a domain write", () => {
    const skipped = renderGuide();
    fireEvent.click(screen.getByRole("button", { name: "Skip for now" }));
    expect(skipped.onDismiss).toHaveBeenCalledOnce();
    expect(readWorkshopPreferences("account-1").onboardingState).toBe("skipped");

    cleanup();
    localStorage.clear();
    const completed = renderGuide();
    fireEvent.click(screen.getByRole("button", { name: "Finish setup" }));
    expect(completed.onDismiss).toHaveBeenCalledOnce();
    expect(readWorkshopPreferences("account-1").onboardingState).toBe("completed");
    expect(readWorkshopPreferences("account-1").onboardingStep).toBe(4);
  });
});
