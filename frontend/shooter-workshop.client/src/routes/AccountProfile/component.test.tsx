import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LanguageSelector } from "../../components/LanguageSelector";
import { I18nProvider } from "../../i18n";
import AccountProfile from "./component";

vi.mock("../../auth/AuthProvider", () => ({
  useWorkshopAuth: () => ({
    session: {
      account: {
        username: "shooter",
        email: "shooter@example.com",
        displayName: "Shooter",
      },
    },
  }),
}));

describe("Account profile localization", () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem("shooter-workshop.culture", "en-GB");
  });

  it("updates page labels immediately when the selected culture changes", () => {
    render(
      <I18nProvider>
        <LanguageSelector />
        <AccountProfile />
      </I18nProvider>,
    );

    expect(screen.getByText("Username")).toBeTruthy();
    fireEvent.change(screen.getByRole("combobox", { name: "Language" }), {
      target: { value: "it-IT" },
    });

    expect(screen.getByText("Nome utente")).toBeTruthy();
    expect(screen.getByText("Identità account MorWalPiz condivisa")).toBeTruthy();
  });
});
