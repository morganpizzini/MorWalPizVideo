import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import type { WorkshopComponentCondition } from "@morwalpizvideo/models";
import { I18nProvider } from "../../i18n";
import FirearmDetail, { MaintenanceFeedback } from "./component";
import type { FirearmDetailLoaderData } from "./loader";

const routers: ReturnType<typeof createMemoryRouter>[] = [];

function makeComponent(id: string, rounds: number, trackingMode: "Counter" | "Interval", serviceInterval?: number): WorkshopComponentCondition {
  return {
    component: {
      id,
      accountId: "account-1",
      definitionId: `definition-${id}`,
      identificationNote: `Part ${id}`,
      compatibilityTags: [],
      totalRounds: rounds,
      serviceInterval,
      trackingMode,
      status: "Installed",
      revision: 1,
      archived: false,
    },
    definition: {
      id: `definition-${id}`,
      accountId: "account-1",
      name: `Component ${id}`,
      trackingMode,
      compatibilityTags: [],
      archived: false,
      revision: 1,
    },
    installation: {
      id: `installation-${id}`,
      accountId: "account-1",
      componentId: id,
      firearmId: "firearm-1",
      installedAt: "2026-01-01T00:00:00Z",
      revision: 1,
    },
  };
}

function makeDetail(components: WorkshopComponentCondition[]): FirearmDetailLoaderData {
  return {
    firearm: {
      id: "firearm-1",
      accountId: "account-1",
      manufacturer: "Example",
      model: "Model",
      caliber: "9mm",
      totalRounds: 750,
      revision: 1,
      archived: true,
    },
    components,
    installationHistory: { items: components, page: 1, pageSize: 15, hasMore: false },
    maintenance: { items: [], page: 1, pageSize: 15, hasMore: false },
    shooting: { items: [], page: 1, pageSize: 15, hasMore: false },
    roundHistory: [],
    replacementCandidates: [],
    projectionState: "Current",
    malfunctionCount: 0,
  };
}

function renderDetail(detail: FirearmDetailLoaderData) {
  const router = createMemoryRouter(
    [{
      id: "firearm",
      path: "/firearms/:id",
      Component: FirearmDetail,
    }],
    {
      initialEntries: ["/firearms/firearm-1"],
      hydrationData: { loaderData: { firearm: { data: Promise.resolve(detail) } } },
    },
  );
  routers.push(router);
  return render(<I18nProvider><RouterProvider router={router} /></I18nProvider>);
}

describe("Firearm detail component usage", () => {
  afterEach(() => {
    cleanup();
    routers.splice(0).forEach((router) => router.dispose());
  });

  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem("shooter-workshop.culture", "en-GB");
  });

  it("compares all installed components and keeps service interval usage separate", async () => {
    renderDetail(makeDetail([
      makeComponent("lower", 250, "Interval", 500),
      makeComponent("highest", 500, "Counter"),
    ]));

    await screen.findByRole("heading", { name: "Installed components", level: 2 });
    const installedDate = screen.getByRole("heading", { name: "Installed components", level: 2 })
      .closest(".panel")?.querySelector(".component-usage-row time");
    expect(installedDate?.getAttribute("dateTime")).toBe("2026-01-01T00:00:00Z");
    expect(installedDate?.parentElement?.textContent).toContain("Installed");
    const relativeMeters = screen.getAllByRole("meter", { name: "Relative use compared with the highest-use component" });
    expect(relativeMeters.map((meter) => meter.getAttribute("aria-valuenow"))).toEqual(["50", "100"]);
    expect(screen.getByRole("meter", { name: "Service interval usage" }).getAttribute("aria-valuenow")).toBe("50");
    expect(screen.getByText("250 / 500 rounds")).toBeTruthy();
    expect(screen.getAllByText("Service interval")).toHaveLength(1);
  });

  it("renders zero-use comparison bars without requiring a service interval", async () => {
    renderDetail(makeDetail([
      makeComponent("first", 0, "Counter"),
      makeComponent("second", 0, "Counter"),
    ]));

    await screen.findByRole("heading", { name: "Installed components", level: 2 });
    const meters = screen.getAllByRole("meter", { name: "Relative use compared with the highest-use component" });
    expect(meters.map((meter) => meter.getAttribute("aria-valuenow"))).toEqual(["0", "0"]);
    expect(screen.queryByText("Service interval")).toBeNull();
  });

  it("pairs the round chart with its ledger table and allows switching views", async () => {
    const detail = makeDetail([makeComponent("installed", 500, "Counter")]);
    detail.roundHistory = [{
      effectiveAt: "2026-01-01T00:00:00Z",
      delta: 500,
      cumulativeRounds: 500,
      operationId: "operation-1",
    }];

    renderDetail(detail);

    await screen.findByRole("heading", { name: "Activity history", level: 2 });
    expect(screen.getByRole("img", { name: /Cumulative round history from/ })).toBeTruthy();
    expect(screen.getByRole("table", { name: "Round history details" })).toBeTruthy();
    const tableButton = screen.getByRole("button", { name: "Table" });
    fireEvent.click(tableButton);
    expect(tableButton.getAttribute("aria-pressed")).toBe("true");
    expect(screen.getByRole("button", { name: "Chart" }).getAttribute("aria-pressed")).toBe("false");
  });

  it("shows prior component installations and their removal dates", async () => {
    const current = makeComponent("current", 500, "Counter");
    const prior = makeComponent("prior", 200, "Counter");
    prior.installation.removedAt = "2026-02-01T00:00:00Z";
    const detail = makeDetail([current]);
    detail.installationHistory = { items: [current, prior], page: 1, pageSize: 1, hasMore: true };

    renderDetail(detail);

    await screen.findByRole("heading", { name: "Component installation history", level: 2 });
    expect(screen.getByText("Component prior")).toBeTruthy();
    expect(screen.getByText("-")).toBeTruthy();
    expect(screen.getByRole("table").textContent).toContain("2026");
    expect(screen.getByRole("link", { name: "Next" }).getAttribute("href")).toBe("/firearms/firearm-1?installationPage=2");
  });

  it("announces a successful maintenance save", async () => {
    render(<I18nProvider><MaintenanceFeedback result={{ ok: true }} submitting={false} /></I18nProvider>);
    expect(screen.getByRole("status").textContent).toContain("Maintenance recorded.");
    cleanup();
    render(<I18nProvider><MaintenanceFeedback result={{ ok: true }} submitting /></I18nProvider>);
    expect(screen.queryByRole("status")).toBeNull();
  });

  it("announces when maintenance is saved locally for synchronization", () => {
    render(<I18nProvider><MaintenanceFeedback result={{ ok: true, offline: true }} submitting={false} /></I18nProvider>);
    expect(screen.getByRole("status").textContent).toContain("Maintenance queued for sync.");
  });
});
