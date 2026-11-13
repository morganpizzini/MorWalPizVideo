import { afterEach, describe, expect, it, vi } from "vitest";
import {
  allocateWorkshopAmmunition,
  releaseWorkshopAllocation,
  reallocateWorkshopAmmunition,
  correctWorkshopShooting,
  getWorkshopBatches,
  recordWorkshopMaintenance,
  recordWorkshopProduction,
  recordWorkshopShooting,
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
      productionDate: "2026-11-13T00:00:00Z",
      quantityProduced: 250,
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
      idempotencyKey: "production-key",
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
      consumptionRecords: [
        { batchId: "batch-1", allocationId: "allocation-1", quantity: 100 },
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
      consumptionRecords: [
        { batchId: "batch-1", allocationId: "allocation-1", quantity: 100 },
      ],
      usageSegments: [
        { componentId: "component-1", installationId: "installation-1", quantity: 150 },
      ],
    });
  });

  it("uses the canonical allocation route and sends optional notes", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) =>
      Promise.resolve(
        String(input).endsWith("/csrf")
          ? Response.json({ token: "allocation-csrf" })
          : Response.json({ id: "operation-3" }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await allocateWorkshopAmmunition({
      batchId: "batch-1",
      firearmId: "firearm-1",
      quantity: 200,
      notes: "training allocation",
      idempotencyKey: "allocation-key",
    });

    expect(String(fetchMock.mock.calls[1][0])).toBe(
      "/api/v1/workshop/ammunition/allocations",
    );
    expect(JSON.parse(String(fetchMock.mock.calls[1][1]?.body))).toEqual({
      batchId: "batch-1",
      firearmId: "firearm-1",
      quantity: 200,
      notes: "training allocation",
      idempotencyKey: "allocation-key",
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

    it("supports allocation release and reallocation commands with encoded identifiers", async () => {
      const fetchMock = vi.fn((input: RequestInfo | URL) =>
        Promise.resolve(
          String(input).endsWith("/csrf")
            ? Response.json({ token: "allocation-change-csrf" })
            : Response.json({ id: "operation-4" }),
        ),
      );
      vi.stubGlobal("fetch", fetchMock);

      await releaseWorkshopAllocation("allocation/1", {
        quantity: 20,
        revision: 3,
        idempotencyKey: "release-key",
      });
      await reallocateWorkshopAmmunition({
        sourceAllocationId: "allocation/1",
        destinationFirearmId: "firearm-2",
        quantity: 10,
        idempotencyKey: "reallocate-key",
      });

      expect(String(fetchMock.mock.calls[1][0])).toBe(
        "/api/v1/workshop/ammunition/allocations/allocation%2F1/release",
      );
      expect(JSON.parse(String(fetchMock.mock.calls[1][1]?.body))).toMatchObject({
        quantity: 20,
        revision: 3,
      });
      expect(String(fetchMock.mock.calls[2][0])).toBe(
        "/api/v1/workshop/ammunition/allocations/reallocate",
      );
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
          consumptionRecords: [],
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
});
