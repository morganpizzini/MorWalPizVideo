import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ToastProvider } from "@morwalpiz/layout/notifications";
import AppShell from "./AppShell";

const testState = vi.hoisted(() => ({
  fetcher: {
    state: "idle",
    data: undefined as unknown,
    submit: vi.fn(),
    formData: null as FormData | null,
  },
  auth: {
    loading: false,
    session: {
      authenticated: true,
      account: { id: "workshop-test", username: "test-user", workshopCapabilityGuideSeenVersion: 1 },
    },
    error: null,
    logout: vi.fn(),
    refresh: vi.fn(),
    updateSession: vi.fn(),
    offline: false,
  },
}));

vi.mock("react-router", () => ({
  NavLink: ({ children, to }: { children: ReactNode; to: string }) => <a href={to}>{children}</a>,
  Outlet: ({ context }: {
    context: {
      offline: {
        busy: boolean;
        preparing: boolean;
        prepare: () => void;
      };
    };
  }) => (
    <button
      type="button"
      disabled={context.offline.busy}
      aria-busy={context.offline.preparing}
      onClick={context.offline.prepare}
    >
      {context.offline.preparing ? "Preparing…" : "Prepare offline"}
    </button>
  ),
  useFetcher: () => testState.fetcher,
  useLocation: () => ({ pathname: "/", search: "", hash: "" }),
  useNavigate: () => vi.fn(),
}));

vi.mock("@morwalpiz/layout", () => ({ AppearanceSelector: () => null }));
vi.mock("@morwalpizvideo/services", () => ({ markWorkshopCapabilityGuideSeen: vi.fn() }));
vi.mock("../auth/AuthProvider", () => ({ useWorkshopAuth: () => testState.auth }));
vi.mock("../components/States", () => ({ LoadingState: () => null }));
vi.mock("../components/CapabilityWizard", () => ({ default: () => null }));
vi.mock("../components/LanguageSelector", () => ({ LanguageSelector: () => null }));
vi.mock("../components/InstallPrompt", () => ({ default: () => null }));
vi.mock("../services/installPrompt", () => ({
  useInstallPrompt: () => ({ canInstall: false, installed: true, reopen: vi.fn() }),
}));
vi.mock("../services/offlineStore", () => ({
  getOfflineState: async () => ({
    pending: 0,
    blocked: 0,
    preparedAt: null,
    storagePersisted: false,
    commands: [],
  }),
  hasOfflineData: async () => false,
  subscribeToOfflineChanges: () => () => {},
}));
vi.mock("../i18n", () => ({
  useI18n: () => ({
    t: (key: string, values?: Record<string, string | number>) =>
      Object.entries(values ?? {}).reduce(
        (text, [name, value]) => text.replace(`{${name}}`, String(value)),
        key,
      ),
  }),
}));

function renderShell() {
  const result = render(<ToastProvider><AppShell /></ToastProvider>);
  return {
    ...result,
    rerenderShell: () => result.rerender(<ToastProvider><AppShell /></ToastProvider>),
  };
}

beforeEach(() => {
  testState.fetcher.state = "idle";
  testState.fetcher.data = undefined;
  testState.fetcher.formData = null;
  testState.fetcher.submit.mockClear();
});

afterEach(cleanup);

describe("offline preparation feedback", () => {
  it("keeps the control busy through loading and announces a successful result once", async () => {
    const { rerenderShell } = renderShell();
    fireEvent.click(screen.getByRole("button", { name: "Prepare offline" }));
    expect(testState.fetcher.submit).toHaveBeenCalledWith(
      { intent: "prepare" },
      { method: "post", action: "/" },
    );

    testState.fetcher.state = "submitting";
    rerenderShell();
    expect(screen.getByRole("button", { name: "Preparing…" }).getAttribute("disabled")).not.toBeNull();
    expect(screen.getByRole("button", { name: "Preparing…" }).getAttribute("aria-busy")).toBe("true");

    testState.fetcher.state = "loading";
    rerenderShell();
    expect(screen.getByRole("button", { name: "Preparing…" }).getAttribute("disabled")).not.toBeNull();

    testState.fetcher.state = "idle";
    testState.fetcher.data = {
      ok: true,
      preparedAt: "2026-11-25T00:00:00.000Z",
      storagePersisted: true,
    };
    rerenderShell();
    await screen.findByText("Offline data prepared.");
    rerenderShell();
    expect(screen.getAllByText("Offline data prepared.")).toHaveLength(1);
  });

  it("keeps preparation failures visible and reports repeated identical failures separately", async () => {
    const { rerenderShell } = renderShell();
    fireEvent.click(screen.getByRole("button", { name: "Prepare offline" }));
    testState.fetcher.state = "submitting";
    rerenderShell();
    testState.fetcher.state = "idle";
    testState.fetcher.data = { ok: false, error: "Preparation failed." };
    rerenderShell();

    await screen.findByText("Preparation failed.");
    rerenderShell();
    expect(screen.getAllByText("Preparation failed.")).toHaveLength(1);

    fireEvent.click(screen.getByRole("button", { name: "Prepare offline" }));
    testState.fetcher.state = "submitting";
    rerenderShell();
    testState.fetcher.state = "idle";
    testState.fetcher.data = { ok: false, error: "Preparation failed." };
    rerenderShell();

    await waitFor(() => expect(screen.getAllByText("Preparation failed.")).toHaveLength(2));
  });

  it("keeps the persistence warning visible until the user dismisses it", async () => {
    const { rerenderShell } = renderShell();
    fireEvent.click(screen.getByRole("button", { name: "Prepare offline" }));
    testState.fetcher.state = "submitting";
    rerenderShell();
    testState.fetcher.state = "idle";
    testState.fetcher.data = {
      ok: true,
      preparedAt: "2026-11-25T00:00:00.000Z",
      storagePersisted: false,
    };
    rerenderShell();

    const warning = await screen.findByText("Offline data prepared, but the browser may clear it.");
    expect(warning).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    await waitFor(() => expect(screen.queryByText("Offline data prepared, but the browser may clear it.")).toBeNull());
  });
});
