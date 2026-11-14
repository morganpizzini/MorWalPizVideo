import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { I18nProvider } from "../i18n";
import { ActivityList, getActivityTitle } from "./shared";

describe("workshop activity presentation", () => {
  it("maps string and numeric operation types to readable titles", () => {
    expect(getActivityTitle("ShootingRecorded")).toBe("Shooting session recorded");
    expect(getActivityTitle(16)).toBe("Shooting session recorded");
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
