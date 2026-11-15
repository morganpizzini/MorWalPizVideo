import { describe, expect, it } from "vitest";
import { routes } from "./config";

describe("Workshop route configuration", () => {
  it("keeps account registration and legal pages outside the protected shell", () => {
    expect(routes.find((route) => route.path === "/account/register")?.element).toBeTruthy();
    expect(routes.find((route) => route.path === "/privacy")?.element).toBeTruthy();
    expect(routes.find((route) => route.path === "/terms")?.element).toBeTruthy();
    expect(routes.find((route) => route.path === "/cookie-policy")?.element).toBeTruthy();
  });

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
      "recipes",
      "production",
      "sessions/shooting",
      "history",
      "integrity",
      "account/profile",
      "*",
    ]);
    const componentsRoute = shellRoute?.children?.find((route) => route.path === "components");
    expect(componentsRoute?.loader).toBeTypeOf("function");
    expect(componentsRoute?.action).toBeTypeOf("function");
    const ammunitionRoute = shellRoute?.children?.find((route) => route.path === "ammunition");
    expect(ammunitionRoute?.loader).toBeTypeOf("function");
    expect(ammunitionRoute?.action).toBeTypeOf("function");
    const productionRoute = shellRoute?.children?.find((route) => route.path === "production");
    expect(productionRoute?.element).toBeTruthy();
    expect(productionRoute?.loader).toBeUndefined();
    expect(productionRoute?.action).toBeUndefined();
  });
});
