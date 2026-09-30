import { describe, expect, it } from "vitest";
import { availabilityMatches, sessionLabel } from "./schedule";
import type { Availability, Session } from "./api";

const morning: Session = {
  periodKey: "morning",
  startUtc: "2030-01-12T08:00:00Z",
  endUtc: "2030-01-12T12:00:00Z",
};
const data: Availability = { ...morning, localDate: "2030-01-12", bays: [] };

describe("server-driven selection", () => {
  it("uses field timezone rather than device timezone", () => {
    expect(sessionLabel(morning, "Europe/Rome")).toBe("Mattina 09:00 - 13:00");
  });
  it("displays hourly intervals without inventing period labels", () => {
    expect(
      sessionLabel(
        { ...morning, periodKey: "09:00", endUtc: "2030-01-12T09:00:00Z" },
        "Europe/Rome",
      ),
    ).toBe("09:00 - 10:00");
  });
  it("invalidates date, mode, session and changed schedule intervals", () => {
    expect(availabilityMatches(data, [morning], "2030-01-12", "morning")).toBe(
      true,
    );
    expect(availabilityMatches(data, [morning], "2030-01-13", "morning")).toBe(
      false,
    );
    expect(availabilityMatches(data, [], "2030-01-12", "morning")).toBe(false);
    expect(
      availabilityMatches(data, [morning], "2030-01-12", "afternoon"),
    ).toBe(false);
    expect(
      availabilityMatches(
        data,
        [{ ...morning, endUtc: "2030-01-12T11:00:00Z" }],
        "2030-01-12",
        "morning",
      ),
    ).toBe(false);
  });
});
