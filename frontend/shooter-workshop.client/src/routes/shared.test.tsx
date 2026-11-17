import { act, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "../i18n";
import { FeedbackProvider } from "../components/feedback/FeedbackProvider";
import { ActivityList, DeferredRoute, getActivityTitle } from "./shared";

vi.mock("../auth/AuthProvider", () => ({
  useWorkshopAuth: () => ({ session: { authenticated: true } }),
}));

describe("workshop activity presentation", () => {
  it("maps string and numeric operation types to readable titles", () => {
    expect(getActivityTitle("ShootingRecorded")).toBe("Shooting session recorded");
    expect(getActivityTitle(22)).toBe("Shooting session recorded");
    expect(getActivityTitle(11)).toBe("Component installed");
  });

  it("uses safe fallbacks and keeps the operation ID secondary", () => {
    render(
      <I18nProvider>
        <ActivityList
          items={[
            {
              id: "activity-1",
              accountId: "account-1",
              operationId: "",
              type: 999 as unknown as string,
              effectiveAt: "2026-11-14T12:00:00Z",
              sourceEntityIds: [],
            },
            {
              id: "activity-2",
              accountId: "account-1",
              operationId: "operation-2",
              type: "FirearmCreated",
              effectiveAt: "2026-11-14T13:00:00Z",
              sourceEntityIds: [],
            },
          ]}
        />
      </I18nProvider>,
    );

    expect(screen.getByText("Workshop activity")).toBeTruthy();
    expect(screen.getByText("Firearm registered")).toBeTruthy();
    expect(screen.getAllByText("Activity recorded")).toHaveLength(2);
    expect(screen.getByText("Operation not set")).toBeTruthy();
    expect(screen.getByText("Operation operation-2")).toBeTruthy();
    expect(screen.queryByText("999")).toBeNull();
    expect(screen.queryByText("undefined")).toBeNull();
  });
});

describe("deferred route data", () => {
  it("renders the route loading state before a loader promise resolves", async () => {
    let resolve!: (value: string) => void;
    const data = new Promise<string>((resolver) => {
      resolve = resolver;
    });

    render(
      <I18nProvider>
        <FeedbackProvider>
          <DeferredRoute data={data}>{(value) => <p>{value}</p>}</DeferredRoute>
        </FeedbackProvider>
      </I18nProvider>,
    );

    expect(screen.getByRole("status").textContent).toContain("Loading workshop data…");
    expect(screen.queryByText("loaded")).toBeNull();

    await act(async () => {
      resolve("loaded");
      await data;
    });

    expect(screen.getByText("loaded")).toBeTruthy();
  });

  it("does not render a framework error while an async error boundary is active", async () => {
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);
    const data = Promise.reject(new Error("request failed"));

    render(
      <I18nProvider>
        <FeedbackProvider>
          <DeferredRoute data={data}>{(value) => <p>{value}</p>}</DeferredRoute>
        </FeedbackProvider>
      </I18nProvider>,
    );

    await screen.findByText("Could not load this workspace.");
    expect(screen.queryByText("Unhandled Application Error")).toBeNull();
    consoleError.mockRestore();
  });
});
