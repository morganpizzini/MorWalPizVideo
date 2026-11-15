import { beforeEach, describe, expect, it, vi } from "vitest";
import type { WorkshopRecipe } from "@morwalpizvideo/models";
import {
  createWorkshopRecipe,
  getWorkshopRecipes,
  updateWorkshopRecipe,
} from "@morwalpizvideo/services";
import { action } from "./action";
import { loader } from "./loader";

vi.mock("@morwalpizvideo/services", () => ({
  createWorkshopRecipe: vi.fn(),
  getWorkshopRecipes: vi.fn(),
  updateWorkshopRecipe: vi.fn(),
}));

const recipes = [
  {
    id: "recipe-1",
    accountId: "account-1",
    name: "Practice",
    caliber: "9x19",
    status: "Active",
    version: 2,
    updatedAt: "2026-11-15T00:00:00Z",
  },
] satisfies WorkshopRecipe[];

const request = (values: Record<string, string>) =>
  new Request("https://workshop.local/recipes", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams(values).toString(),
  });

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("crypto", { randomUUID: () => "recipe-key" });
});

describe("Recipes route data functions", () => {
  it("loads the account recipes", async () => {
    vi.mocked(getWorkshopRecipes).mockResolvedValue(recipes);
    await expect(loader()).resolves.toEqual({ recipes });
  });

  it("submits every create field", async () => {
    vi.mocked(createWorkshopRecipe).mockResolvedValue(recipes[0]);

    await expect(
      action({
        request: request({
          name: "Practice",
          caliber: "9x19",
          oal: "29.5",
          bulletWeight: "124",
          bulletForm: "FMJ",
          drawing: "profile-a",
          primer: "Small pistol",
          powder: "Powder A",
          powderWeight: "4.2",
          notes: "range load",
        }),
      } as never),
    ).resolves.toEqual({ ok: true, intent: "create" });

    expect(createWorkshopRecipe).toHaveBeenCalledWith({
      name: "Practice",
      caliber: "9x19",
      oal: "29.5",
      bulletWeight: 124,
      bulletForm: "FMJ",
      drawing: "profile-a",
      primer: "Small pistol",
      powder: "Powder A",
      powderWeight: 4.2,
      notes: "range load",
    });
  });

  it("updates a recipe with its version and idempotency key", async () => {
    vi.mocked(updateWorkshopRecipe).mockResolvedValue(recipes[0]);

    await expect(
      action({
        request: request({
          intent: "update",
          recipeId: "recipe-1",
          version: "2",
          name: "Practice revised",
          caliber: "9x19",
          oal: "29.5",
          bulletForm: "FMJ",
          drawing: "",
          primer: "Small pistol",
          powder: "Powder A",
          bulletWeight: "",
          powderWeight: "",
          notes: "",
        }),
      } as never),
    ).resolves.toEqual({ ok: true, intent: "update" });

    expect(updateWorkshopRecipe).toHaveBeenCalledWith("recipe-1", {
      name: "Practice revised",
      caliber: "9x19",
      oal: "29.5",
      bulletWeight: null,
      bulletForm: "FMJ",
      drawing: null,
      primer: "Small pistol",
      powder: "Powder A",
      powderWeight: null,
      notes: null,
      version: 2,
      idempotencyKey: "recipe-key",
    });
  });

  it("rejects incomplete or invalid forms without submitting", async () => {
    await expect(
      action({
        request: request({
          name: "Practice",
          caliber: "9x19",
          bulletWeight: "not-a-number",
        }),
      } as never),
    ).resolves.toEqual({
      ok: false,
      error: "Please review the form.",
      intent: "create",
    });
    expect(createWorkshopRecipe).not.toHaveBeenCalled();
  });

  it("returns service failures as form feedback", async () => {
    vi.mocked(updateWorkshopRecipe).mockRejectedValue(
      new Error("Recipe changed. Reload before updating."),
    );

    await expect(
      action({
        request: request({
          intent: "update",
          recipeId: "recipe-1",
          version: "2",
          name: "Practice",
          caliber: "9x19",
        }),
      } as never),
    ).resolves.toEqual({
      ok: false,
      error: "Recipe changed. Reload before updating.",
      intent: "update",
    });
  });
});
