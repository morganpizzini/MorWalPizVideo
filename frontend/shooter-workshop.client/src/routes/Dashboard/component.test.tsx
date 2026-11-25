import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { WorkshopAnalyticsBucket } from "@morwalpizvideo/models";
import { MemoryRouter } from "react-router";
import Dashboard, { DailyActivityTable } from "./component";

const testState = vi.hoisted(() => ({ loaderData: null as unknown, outletContext: null as unknown }));

vi.mock("react-router", async importOriginal => {
  const actual = await importOriginal<typeof import("react-router")>();
  return {
    ...actual,
    useLoaderData: () => testState.loaderData,
    useOutletContext: () => testState.outletContext,
  };
});

vi.mock("../shared", async importOriginal => {
  const actual = await importOriginal<typeof import("../shared")>();
  return { ...actual, DeferredRoute: ({ fallback }: { fallback?: React.ReactNode }) => fallback };
});

vi.mock("../../i18n", () => ({
  useI18n: () => ({
    culture: "en-GB",
    t: (key: string, values?: Record<string, string | number>) =>
      Object.entries(values ?? {}).reduce(
        (text, [name, value]) => text.replace(`{${name}}`, String(value)),
        key,
      ),
  }),
}));

afterEach(cleanup);

function renderDashboard(offline: {
  effectivelyOffline: boolean;
  preparedAt: string | null;
  pending: number;
  busy: boolean;
  preparing: boolean;
  prepare: () => void;
  sync: () => void;
}) {
  const never = new Promise<never>(() => {});
  testState.loaderData = { dashboard: never, history: never, analytics: never, days: 30 };
  testState.outletContext = { offline };
  return render(<MemoryRouter><Dashboard /></MemoryRouter>);
}

describe("dashboard offline readiness", () => {
  it("keeps preparation controls in the hero and shows the full pending state", async () => {
    const prepare = vi.fn();
    const sync = vi.fn();
    renderDashboard({
      effectivelyOffline: false,
      preparedAt: null,
      pending: 2,
      busy: true,
      preparing: true,
      prepare,
      sync,
    });

    const prepareButton = screen.getByRole("button", { name: "Preparing…" });
    const hero = screen.getByRole("region", { name: "Offline readiness" });
    expect(hero.closest(".dashboard-hero")).not.toBeNull();
    expect(prepareButton.getAttribute("disabled")).not.toBeNull();
    expect(prepareButton.getAttribute("aria-busy")).toBe("true");
    expect(prepareButton.querySelector(".loading-spinner")).not.toBeNull();
    expect(within(hero).getByText("2 pending")).not.toBeNull();
    expect(within(hero).getByRole("button", { name: "Sync now" }).getAttribute("disabled")).not.toBeNull();
    expect(screen.queryByText("Offline readiness", { selector: ".offline-status strong" })).toBeNull();
  });

  it("submits preparation from the dashboard hero when idle", () => {
    const prepare = vi.fn();
    renderDashboard({
      effectivelyOffline: false,
      preparedAt: "2026-11-25T00:00:00.000Z",
      pending: 0,
      busy: false,
      preparing: false,
      prepare,
      sync: vi.fn(),
    });

    fireEvent.click(screen.getByRole("button", { name: "Prepare offline" }));
    expect(prepare).toHaveBeenCalledOnce();
  });
});

describe("daily activity table", () => {
  it("combines production and use, shows multi-session counts, and joins notes", () => {
    const buckets: WorkshopAnalyticsBucket[] = [{
      date: "2026-11-14T00:00:00.000Z",
      shootingRounds: 250,
      producedRounds: 1200,
      shootingSessions: 2,
      notes: ["Practice", "Batch run"],
    }];

    render(<DailyActivityTable buckets={buckets} />);

    const row = within(screen.getByRole("table")).getAllByRole("row")[1];
    expect(within(row).getByText("14 Nov 2026 (2)")).toBeTruthy();
    expect(within(row).getByText("+1,200")).toBeTruthy();
    expect(within(row).getByText("-250")).toBeTruthy();
    expect(within(row).getByText("Practice · Batch run")).toBeTruthy();
  });

  it("omits a single-session count and uses a dash when notes are absent", () => {
    const buckets: WorkshopAnalyticsBucket[] = [{
      date: "2026-11-13T00:00:00.000Z",
      shootingRounds: 75,
      producedRounds: 0,
      shootingSessions: 1,
    }];

    render(<DailyActivityTable buckets={buckets} />);

    const row = within(screen.getByRole("table")).getAllByRole("row")[1];
    expect(within(row).getByText("13 Nov 2026")).toBeTruthy();
    expect(within(row).queryByText("13 Nov 2026 (1)")).toBeNull();
    expect(within(row).getByText("-", { exact: true })).toBeTruthy();
  });
});
