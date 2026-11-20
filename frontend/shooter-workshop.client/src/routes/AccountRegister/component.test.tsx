import { renderToStaticMarkup } from "react-dom/server";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import AccountRegister from "./component";
import { I18nProvider } from "../../i18n";

vi.mock("../../auth/AuthProvider", () => ({
  useWorkshopAuth: () => ({ refresh: vi.fn() }),
}));

vi.mock("@morwalpizvideo/services", () => ({
  getApiFieldErrors: () => ({}),
  getPublicAccountErrorMessage: () => "Unable to complete the request.",
  registerPublicAccount: vi.fn(),
}));

describe("Workshop account registration", () => {
  it("renders the shared account form with legal acknowledgement", () => {
    const markup = renderToStaticMarkup(
      <MemoryRouter
        initialEntries={["/account/register?returnUrl=%2Ffirearms"]}
      >
        <I18nProvider>
          <AccountRegister />
        </I18nProvider>
      </MemoryRouter>,
    );

    expect(markup).toContain("Create your Workshop account");
    expect(markup).toContain('type="checkbox"');
    expect(markup).toContain(
      "I have read the privacy notice and accept the terms of use.",
    );
    expect(markup).toContain('class="mpv-appearance__trigger"');
    expect(markup).toContain('role="menuitemradio"');
    expect(markup).not.toContain('class="mpv-appearance__select"');
  });
});
