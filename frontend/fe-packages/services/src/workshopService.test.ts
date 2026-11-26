import { afterEach, describe, expect, it, vi } from "vitest";
import {
  correctWorkshopShooting,
  getWorkshopBatches,
  getWorkshopInventory,
  getWorkshopProductionRecords,
  createAndInstallWorkshopComponent,
  installWorkshopComponent,
  removeWorkshopComponent,
  createWorkshopComponentDefinition,
  recordWorkshopMaintenance,
  recordWorkshopProduction,
  updateWorkshopProduction,
  recordWorkshopShooting,
  getWorkshopHistory,
  getWorkshopFirearmDetail,
} from "./workshopService";
import { accountApiService } from "./apiTransport";

afterEach(() => {
  vi.unstubAllGlobals();
  accountApiService.resetCsrfToken();
});

describe("Workshop account transport", () => {
  it("uses the canonical batches route and account credentials without channel or bearer headers", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      Response.json([
        {
          id: "batch-1",
          accountId: "account-1",
          recipeId: "recipe-1",
          recipeSnapshot: { name: "Practice", caliber: "9mm", version: 1 },
          productionSessionId: "production-1",
          productionDate: "2026-11-13T00:00:00Z",
          producedQuantity: 100,
          onHandQuantity: 100,
          revision: 0,
        },
      ]),
    );
    vi.stubGlobal("fetch", fetchMock);

    await getWorkshopBatches();

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(String(fetchMock.mock.calls[0][0])).toBe(
      "/api/v1/workshop/ammunition/batches",
    );
    const options = fetchMock.mock.calls[0][1];
    expect(options?.credentials).toBe("include");
    const headers = new Headers(options?.headers);
    expect(headers.get("Authorization")).toBeNull();
    expect(headers.get("X-Channel-Id")).toBeNull();
  });

  it("posts production using server DTO field names and acquires CSRF through account mode", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) =>
      Promise.resolve(
        String(input).endsWith("/csrf")
          ? Response.json({ token: "workshop-csrf" })
          : Response.json({
              id: "operation-1",
              operationType: "ProductionRecorded",
              effectiveAt: "2026-11-13T00:00:00Z",
              recordedAt: "2026-11-13T00:00:01Z",
              contributionDigest: "digest",
            }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await recordWorkshopProduction({
      recipeId: "recipe-1",
      recipeVersion: 1,
      productionDate: "2026-11-13T00:00:00Z",
      quantityProduced: 250,
      bulletProductId: "bullet-product",
      primerLotAllocations: [{ lotId: "primer-lot", quantity: 250 }],
      bulletLotAllocations: [{ lotId: "bullet-lot", quantity: 250 }],
      notes: "range session",
      idempotencyKey: "production-key",
    });

    expect(String(fetchMock.mock.calls[1][0])).toBe(
      "/api/v1/workshop/ammunition/production",
    );
    const request = fetchMock.mock.calls[1][1];
    expect(new Headers(request?.headers).get("X-CSRF-TOKEN")).toBe("workshop-csrf");
    expect(JSON.parse(String(request?.body))).toMatchObject({
      recipeId: "recipe-1",
      productionDate: "2026-11-13T00:00:00Z",
      quantityProduced: 250,
      bulletProductId: "bullet-product",
      primerLotAllocations: [{ lotId: "primer-lot", quantity: 250 }],
      idempotencyKey: "production-key",
    });
  });

  it("uses account transport for online inventory reads and production-record prefill", async () => {
    const fetchMock = vi.fn().mockImplementation(() =>
      Promise.resolve(Response.json({
        products: [],
        lots: [],
        componentPurchases: [],
        spending: [],
        partialConsumptionCosts: [],
        missingComponentPrices: [],
      })),
    );
    vi.stubGlobal("fetch", fetchMock);

    await getWorkshopInventory();
    await getWorkshopProductionRecords();

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      "/api/v1/workshop/inventory",
      expect.objectContaining({ credentials: "include" }),
    );
    expect(String(fetchMock.mock.calls[1][0])).toBe(
      "/api/v1/workshop/ammunition/production",
    );
  });

  it("updates a production record with its expected revision and selected lot allocations", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) =>
      Promise.resolve(
        String(input).endsWith("/csrf")
          ? Response.json({ token: "production-edit-csrf" })
          : Response.json({ id: "operation-edit" }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await updateWorkshopProduction("session/1", {
      recipeId: "recipe-1",
      recipeVersion: 2,
      productionDate: "2026-11-14T00:00:00Z",
      quantityProduced: 75,
      bulletProductId: "bullet-product",
      primerLotAllocations: [{ lotId: "primer-lot", quantity: 75 }],
      bulletLotAllocations: [{ lotId: "bullet-lot", quantity: 75 }],
      notes: "corrected",
      idempotencyKey: "production-edit-key",
      expectedRevision: 3,
    });

    expect(String(fetchMock.mock.calls[1][0])).toBe(
      "/api/v1/workshop/ammunition/production/session%2F1",
    );
    expect(new Headers(fetchMock.mock.calls[1][1]?.headers).get("X-CSRF-TOKEN"))
      .toBe("production-edit-csrf");
    expect(JSON.parse(String(fetchMock.mock.calls[1][1]?.body))).toMatchObject({
      expectedRevision: 3,
      quantityProduced: 75,
      primerLotAllocations: [{ lotId: "primer-lot", quantity: 75 }],
    });
  });

  it("preserves explicit untracked ammunition and installation usage segments", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) =>
      Promise.resolve(
        String(input).endsWith("/csrf")
          ? Response.json({ token: "shooting-csrf" })
          : Response.json({ id: "operation-2" }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await recordWorkshopShooting({
      firearmId: "firearm-1",
      date: "2026-11-13T00:00:00Z",
      totalRounds: 150,
      notes: "mixed consumption",
      untrackedAmmunition: 50,
      ammunitionRecords: [
        { recipeId: "recipe-1", quantity: 100 },
      ],
      usageSegments: [
        { componentId: "component-1", installationId: "installation-1", quantity: 150 },
      ],
      idempotencyKey: "shooting-key",
    });

    const body = JSON.parse(String(fetchMock.mock.calls[1][1]?.body));
    expect(String(fetchMock.mock.calls[1][0])).toBe(
      "/api/v1/workshop/sessions/shooting",
    );
    expect(body).toMatchObject({
      untrackedAmmunition: 50,
      ammunitionRecords: [
        { recipeId: "recipe-1", quantity: 100 },
      ],
      usageSegments: [
        { componentId: "component-1", installationId: "installation-1", quantity: 150 },
      ],
    });
  });

  it("records maintenance through the account-protected command route", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) =>
      Promise.resolve(
        String(input).endsWith("/csrf")
          ? Response.json({ token: "maintenance-csrf" })
          : Response.json({ id: "maintenance-1" }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await recordWorkshopMaintenance({
      componentId: "component-1",
      eventType: "Inspection",
      date: "2026-11-13T00:00:00Z",
      roundCountAtEvent: 125,
      description: "Inspect component",
      idempotencyKey: "maintenance-key",
    });

    expect(String(fetchMock.mock.calls[1][0])).toBe(
      "/api/v1/workshop/maintenance",
    );
    expect(JSON.parse(String(fetchMock.mock.calls[1][1]?.body))).toMatchObject({
      componentId: "component-1",
      eventType: "Inspection",
      idempotencyKey: "maintenance-key",
    });
  });

  it("encodes bounded history filters as query parameters", async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json([]));
    vi.stubGlobal("fetch", fetchMock);

    await getWorkshopHistory({ page: 2, pageSize: 50, type: "ShootingRecorded" });

    expect(String(fetchMock.mock.calls[0][0])).toBe(
      "/api/v1/workshop/history?page=2&pageSize=50&type=ShootingRecorded",
    );
  });

  it.each([
    [0, "Current"],
    [1, "Pending"],
    [2, "Inconsistent"],
    [9, "Unknown"],
  ] as const)("normalizes the firearm projection state %s", async (wireState, expectedState) => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ projectionState: wireState }));
    vi.stubGlobal("fetch", fetchMock);

    const detail = await getWorkshopFirearmDetail("firearm-1", { installationPage: 2 });

    expect(detail.projectionState).toBe(expectedState);
    expect(String(fetchMock.mock.calls[0][0])).toContain("installationPage=2");
  });

    it("posts a shooting correction to the append-only correction route", async () => {
      const fetchMock = vi.fn((input: RequestInfo | URL) =>
        Promise.resolve(
          String(input).endsWith("/csrf")
            ? Response.json({ token: "correction-csrf" })
            : Response.json({ id: "operation-5" }),
        ),
      );
      vi.stubGlobal("fetch", fetchMock);

      await correctWorkshopShooting({
        originalOperationId: "shooting-1",
        replacementShooting: {
          firearmId: "firearm-1",
          date: "2026-11-13T00:00:00Z",
          totalRounds: 50,
          untrackedAmmunition: 50,
          ammunitionRecords: [],
          usageSegments: [],
          idempotencyKey: "replacement-key",
        },
        idempotencyKey: "correction-key",
      });

      expect(String(fetchMock.mock.calls[1][0])).toBe(
        "/api/v1/workshop/sessions/shooting/correct",
      );
      expect(JSON.parse(String(fetchMock.mock.calls[1][1]?.body))).toMatchObject({
        originalOperationId: "shooting-1",
        replacementShooting: { totalRounds: 50 },
        idempotencyKey: "correction-key",
      });
    });

    it("uses the compatibility and installation command routes with account CSRF", async () => {
        const fetchMock = vi.fn((input: RequestInfo | URL) =>
          Promise.resolve(
            String(input).endsWith("/csrf")
              ? Response.json({ token: "component-csrf" })
              : Response.json({ component: { id: "component-1" }, installation: { id: "installation-1" } }),
          ),
        );
        vi.stubGlobal("fetch", fetchMock);

        await createWorkshopComponentDefinition({
          name: "Trigger",
          trackingMode: "Counter",
          compatibilityTags: ["9mm", "pistol"],
        });
        await createAndInstallWorkshopComponent({
          definitionId: "definition-1",
          firearmId: "firearm-1",
          installedAt: "2026-11-14T00:00:00Z",
          idempotencyKey: "create-install-key",
        });
        await installWorkshopComponent({
          componentId: "component-1",
          firearmId: "firearm-1",
          installedAt: "2026-11-14T00:00:00Z",
          idempotencyKey: "install-key",
        });
        await removeWorkshopComponent("installation/1", {
          removedAt: "2026-11-14T01:00:00Z",
          revision: 1,
          idempotencyKey: "remove-key",
        });

        expect(JSON.parse(String(fetchMock.mock.calls[1][1]?.body))).toMatchObject({
          compatibilityTags: ["9mm", "pistol"],
        });
        expect(String(fetchMock.mock.calls[2][0])).toBe(
          "/api/v1/workshop/components/install",
        );
        expect(String(fetchMock.mock.calls[3][0])).toBe(
          "/api/v1/workshop/components/installations",
        );
        expect(String(fetchMock.mock.calls[4][0])).toBe(
          "/api/v1/workshop/components/installations/installation%2F1/remove",
        );
        expect(new Headers(fetchMock.mock.calls[4][1]?.headers).get("X-CSRF-TOKEN")).toBe(
          "component-csrf",
        );
    });
  });
