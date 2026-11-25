import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { I18nProvider, useI18n } from "../i18n";
import { resetInstallPromptForTests } from "../services/installPrompt";
import InstallPrompt from "./InstallPrompt";

function SelectItalian() {
  const { setCulture } = useI18n();
  return <button onClick={() => setCulture("it-IT")}>Use Italian</button>;
}

describe("Workshop install invitation", () => {
  it("shows accessible recovery feedback in Italian when the browser prompt fails", async () => {
    resetInstallPromptForTests();
    const prompt = vi.fn().mockRejectedValue(new Error("prompt unavailable"));
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);
    window.dispatchEvent(
      Object.assign(new Event("beforeinstallprompt"), {
        prompt,
        userChoice: Promise.resolve({ outcome: "accepted" as const }),
      }),
    );

    render(
      <I18nProvider>
        <SelectItalian />
        <InstallPrompt />
      </I18nProvider>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Use Italian" }));
    fireEvent.click(screen.getByRole("button", { name: "Installa" }));

    expect((await screen.findByRole("alert")).textContent).toBe(
      "Non è stato possibile avviare l'installazione. Usa l'opzione di installazione del browser per riprovare.",
    );
    expect(screen.queryByRole("button", { name: "Installa" })).toBeNull();
    expect(screen.getByRole("button", { name: "Non ora" })).toBeTruthy();

    consoleError.mockRestore();
    resetInstallPromptForTests();
  });
});
