import { describe, expect, it } from "vitest";
import { routes } from "./config";

describe("Workshop route configuration", () => {
  it("keeps the complete operational navigation under the protected shell", () => {
    const shellRoute = routes.find((route) => route.path === "/");
    const childPaths = shellRoute?.children
      ?.map((route) => route.path)
      .filter((path): path is string => typeof path === "string");

    expect(childPaths).toEqual([
      "firearms",
      "firearms/:id",
      "components",
      "maintenance",
      "ammunition",
      "ammunition/allocations",
      "recipes",
      "production",
      "sessions/shooting",
      "history",
      "integrity",
      "account/profile",
      "*",
    ]);
  });
});
