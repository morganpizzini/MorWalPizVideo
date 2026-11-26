import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import type { WorkshopRecipe } from "@morwalpizvideo/models";
import { I18nProvider } from "../../i18n";
import Recipes from "./component";

const recipes: WorkshopRecipe[] = [
  {
    id: "recipe-1",
    accountId: "account-1",
    name: "Practice",
    caliber: "9x19",
    primerProductId: "primer-1",
    powderProductId: "powder-1",
    status: "Active",
    version: 1,
    updatedAt: "2026-11-15T00:00:00Z",
  },
];

afterEach(cleanup);

describe("Recipes", () => {
  it("keeps recipe viewing available while online inventory is unavailable", () => {
    render(
      <I18nProvider>
        <Recipes recipes={recipes} inventory={null} embedded />
      </I18nProvider>,
    );

    expect(screen.getByRole("heading", { name: "Practice" })).toBeTruthy();
    expect((screen.getByRole("button", { name: "+ New recipe" }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText("Inventory must be available online to edit recipe products.")).toBeTruthy();
  });
});
