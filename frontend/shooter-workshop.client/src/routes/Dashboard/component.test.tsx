import { cleanup, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { WorkshopAnalyticsBucket } from "@morwalpizvideo/models";
import { DailyActivityTable } from "./component";

vi.mock("../../i18n", () => ({
  useI18n: () => ({ culture: "en-GB", t: (key: string) => key }),
}));

afterEach(cleanup);

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
