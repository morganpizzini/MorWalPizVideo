import { beforeEach, describe, expect, it, vi } from "vitest";
import { getWorkshopRecipe, recordWorkshopProduction } from "@morwalpizvideo/services";
import { action } from "./action";
import { loader } from "./loader";

vi.mock("@morwalpizvideo/services", () => ({
  getWorkshopRecipe: vi.fn(),
  recordWorkshopProduction: vi.fn(),
}));

const request = (values: Record<string, string>) =>
  new Request("https://workshop.local/recipes/recipe-1/produce", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams(values),
  });

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("crypto", { randomUUID: () => "production-key" });
});

describe("Recipe production route data functions", () => {
  it("loads the account-scoped recipe", async () => {
    const recipe = { id: "recipe-1", status: "Active" };
    vi.mocked(getWorkshopRecipe).mockResolvedValue(recipe as never);

    await expect(loader({ params: { recipeId: "recipe-1" } } as never)).resolves.toBe(recipe);
    expect(getWorkshopRecipe).toHaveBeenCalledWith("recipe-1");
  });

  it("records production through the existing idempotent command", async () => {
    vi.mocked(recordWorkshopProduction).mockResolvedValue({} as never);

    await expect(
      action({
        request: request({
          recipeId: "recipe-1",
          productionDate: "2026-03-20",
          quantityProduced: "50",
          notes: "range batch",
        }),
      } as never),
    ).resolves.toEqual({ ok: true });

    expect(recordWorkshopProduction).toHaveBeenCalledWith({
      recipeId: "recipe-1",
      productionDate: "2026-03-20",
      quantityProduced: 50,
      notes: "range batch",
      idempotencyKey: "production-key",
    });
  });

  it("rejects an invalid production draft without submitting", async () => {
    await expect(
      action({ request: request({ recipeId: "recipe-1", quantityProduced: "0" }) } as never),
    ).resolves.toEqual({ ok: false, error: "Please review the form." });
    expect(recordWorkshopProduction).not.toHaveBeenCalled();
  });
});
