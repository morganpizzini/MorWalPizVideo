import { describe, expect, it } from "vitest";
import { getSafeReturnPath } from "./component";

describe("Workshop login return paths", () => {
  it("preserves a local protected path, query, and hash", () => {
    expect(getSafeReturnPath("/firearms/firearm-1?tab=history#notes")).toBe(
      "/firearms/firearm-1?tab=history#notes",
    );
  });

  it("rejects external and protocol-relative destinations", () => {
    expect(getSafeReturnPath("https://evil.example/steal")).toBe("/");
    expect(getSafeReturnPath("//evil.example/steal")).toBe("/");
  });

  it("rejects backslash-based ambiguous destinations", () => {
    expect(getSafeReturnPath("/\\evil.example/steal")).toBe("/");
  });
});
