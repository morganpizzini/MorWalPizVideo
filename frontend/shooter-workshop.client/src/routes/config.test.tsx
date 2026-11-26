import { describe, expect, it } from "vitest";
import { routes } from "./config";

describe("Workshop route configuration", () => {
  it("keeps account registration and legal pages outside the protected shell", () => {
    expect(routes.find((route) => route.path === "/account/login")?.lazy).toBeTypeOf("function");
    expect(routes.find((route) => route.path === "/account/register")?.lazy).toBeTypeOf("function");
    expect(routes.find((route) => route.path === "/privacy")?.element).toBeTruthy();
    expect(routes.find((route) => route.path === "/terms")?.element).toBeTruthy();
    expect(routes.find((route) => route.path === "/cookie-policy")?.element).toBeTruthy();
  });

  it("keeps the complete operational navigation under the protected shell", () => {
    const shellRoute = routes.find((route) => route.path === "/");
    expect(shellRoute?.errorElement).toBeTruthy();
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
      "recipes/:recipeId/produce",
      "production",
      "sessions/shooting",
      "history",
      "inventory",
      "integrity",
      "account/profile",
      "*",
    ]);
    const componentsRoute = shellRoute?.children?.find((route) => route.path === "components");
    expect(componentsRoute?.lazy).toBeTypeOf("function");
    expect(componentsRoute?.loader).toBeUndefined();
    expect(componentsRoute?.action).toBeUndefined();
    expect(componentsRoute?.element).toBeUndefined();
    const ammunitionRoute = shellRoute?.children?.find((route) => route.path === "ammunition");
    expect(ammunitionRoute?.lazy).toBeTypeOf("function");
    expect(ammunitionRoute?.loader).toBeUndefined();
    expect(ammunitionRoute?.action).toBeUndefined();
    const productionRoute = shellRoute?.children?.find((route) => route.path === "production");
    expect(productionRoute?.element).toBeTruthy();
    expect(productionRoute?.loader).toBeUndefined();
    expect(productionRoute?.action).toBeUndefined();
    const inventoryRoute = shellRoute?.children?.find((route) => route.path === "inventory");
    expect(inventoryRoute?.lazy).toBeTypeOf("function");
    expect(inventoryRoute?.loader).toBeUndefined();
    expect(inventoryRoute?.action).toBeUndefined();
    for (const path of ["firearms", "sessions/shooting", "history", "integrity"]) {
      const route = shellRoute?.children?.find((candidate) => candidate.path === path);
      expect(route?.lazy, `${path} lazy resolver`).toBeTypeOf("function");
      expect(route?.loader, `${path} eager loader`).toBeUndefined();
    }
    const firearmsRoute = shellRoute?.children?.find((route) => route.path === "firearms");
    expect(firearmsRoute?.lazy).toBeTypeOf("function");
    expect(firearmsRoute?.action).toBeUndefined();
    expect(firearmsRoute?.shouldRevalidate).toBeUndefined();
    expect(shellRoute?.children?.find((route) => route.path === "maintenance")?.element).toBeTruthy();
    expect(shellRoute?.children?.find((route) => route.path === "sessions/shooting")?.lazy).toBeTypeOf("function");
    expect(shellRoute?.children?.find((route) => route.path === "integrity")?.lazy).toBeTypeOf("function");
  });
});
