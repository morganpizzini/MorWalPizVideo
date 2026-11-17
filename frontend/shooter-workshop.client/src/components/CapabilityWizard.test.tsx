import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "../i18n";
import CapabilityWizard from "./CapabilityWizard";

describe("CapabilityWizard", () => {
  afterEach(cleanup);

  function renderWizard() {
    const onComplete = vi.fn();
    const onClose = vi.fn();
    const onSkip = vi.fn();
    render(
      <I18nProvider>
        <MemoryRouter>
          <CapabilityWizard onClose={onClose} onSkip={onSkip} onComplete={onComplete} />
        </MemoryRouter>
      </I18nProvider>,
    );
    return { onComplete, onClose, onSkip };
  }

  it("moves through capability steps and completes", () => {
    const { onComplete } = renderWizard();

    expect(screen.getByRole("dialog")).toBeTruthy();
    expect(screen.getByText("Welcome to Workshop")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    expect(screen.getByText("Equipment and maintenance records")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    fireEvent.click(screen.getByRole("button", { name: "Finish guide" }));

    expect(onComplete).toHaveBeenCalledOnce();
  });

  it("supports skip and escape dismissal", () => {
    const { onClose, onSkip } = renderWizard();

    fireEvent.click(screen.getByRole("button", { name: "Skip for now" }));
    expect(onSkip).toHaveBeenCalledOnce();
    fireEvent.keyDown(document, { key: "Escape" });
    expect(onClose).toHaveBeenCalledOnce();
  });
});
