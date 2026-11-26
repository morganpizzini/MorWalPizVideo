import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { createContext, forwardRef, useContext, useState, type ComponentProps } from "react";
import type { FetcherWithComponents } from "react-router";
import { afterEach, describe, expect, it } from "vitest";
import { I18nProvider } from "../../i18n";
import type { WorkshopInventory, WorkshopRecipe } from "@morwalpizvideo/models";
import type { ProductionActionResult } from "../submitProduction";
import ProductionForm from "../ProductionForm";
import AmmunitionPanel from "./AmmunitionPanel";

type AmmunitionFetcher = FetcherWithComponents<ProductionActionResult>;

const inventory: WorkshopInventory = {
  products: [
    { id: "primer-1", type: "Primer", name: "Primer", archived: false },
    { id: "powder-1", type: "Powder", name: "Powder", archived: false },
    { id: "bullet-1", type: "Bullet", name: "Bullet", archived: false },
  ],
  lots: [
    { id: "primer-lot", productId: "primer-1", purchaseDate: "2026-01-01", purchasedQuantity: 100, availableQuantity: 100, totalPrice: 10, currency: "EUR", isEmpty: false, revision: 1 },
    { id: "bullet-lot", productId: "bullet-1", purchaseDate: "2026-01-01", purchasedQuantity: 100, availableQuantity: 100, totalPrice: 20, currency: "EUR", isEmpty: false, revision: 1 },
  ],
  componentPurchases: [],
  spending: [],
  partialConsumptionCosts: [],
  missingComponentPrices: [],
};

const SubmitFormContext = createContext<() => void>(() => {});

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

const TestFetcherForm = forwardRef<HTMLFormElement, ComponentProps<"form">>((props, ref) => {
  const submit = useContext(SubmitFormContext);
  return (
    <form
      {...props}
      ref={ref}
      onSubmit={(event) => {
        event.preventDefault();
        submit();
      }}
    />
  );
});

function ProductionFormHarness({ result }: { result: ProductionActionResult }) {
  const [state, setState] = useState<AmmunitionFetcher["state"]>("idle");
  const [data, setData] = useState<AmmunitionFetcher["data"]>();
  const fetcher = { state, data, Form: TestFetcherForm } as AmmunitionFetcher;
  const submit = () => {
    setState("submitting");
    window.setTimeout(() => {
      setData(result);
      setState("idle");
    }, 0);
  };

  return (
    <I18nProvider>
      <SubmitFormContext.Provider value={submit}>
        <AmmunitionPanel
          batches={[]}
          productionRecords={[]}
          renderProductionEntry={(record, minimumQuantity, onSaved, onCancel) => (
            <ProductionForm
              recipes={recipes}
              inventory={inventory}
              fetcher={fetcher}
              record={record}
              minimumQuantity={minimumQuantity}
              onSaved={onSaved}
              onCancel={onCancel}
            />
          )}
        />
      </SubmitFormContext.Provider>
    </I18nProvider>
  );
}

afterEach(cleanup);

describe("AmmunitionPanel", () => {
  it("resets the production form after a successful submission", async () => {
    render(<ProductionFormHarness result={{ ok: true, intent: "produce" }} />);

    const recipe = screen.getByLabelText("Recipe") as HTMLSelectElement;
    const quantity = screen.getByLabelText("Quantity produced") as HTMLInputElement;
    const productionDate = screen.getByLabelText("Production date") as HTMLInputElement;
    const notes = screen.getByLabelText("Notes") as HTMLTextAreaElement;
    fireEvent.change(recipe, { target: { value: "recipe-1" } });
    fireEvent.change(screen.getByLabelText("Bullet product"), { target: { value: "bullet-1" } });
    fireEvent.change(quantity, { target: { value: "25" } });
    fireEvent.change(notes, { target: { value: "Test batch" } });
    fireEvent.click(screen.getByRole("button", { name: "Create batch" }));

    await screen.findByText("Production recorded.");
    await waitFor(() => {
      expect(recipe.value).toBe("");
      expect(quantity.value).toBe("");
      expect(productionDate.value).toBe(productionDate.defaultValue);
      expect(notes.value).toBe("");
    });
  });

  it("keeps the entered values when submission fails", async () => {
    render(<ProductionFormHarness result={{
      ok: false,
      error: "Save failed.",
      uncertain: false,
      intent: "produce",
      idempotencyKey: "test-key",
    }} />);

    const recipe = screen.getByLabelText("Recipe") as HTMLSelectElement;
    const quantity = screen.getByLabelText("Quantity produced") as HTMLInputElement;
    const notes = screen.getByLabelText("Notes") as HTMLTextAreaElement;
    fireEvent.change(recipe, { target: { value: "recipe-1" } });
    fireEvent.change(screen.getByLabelText("Bullet product"), { target: { value: "bullet-1" } });
    fireEvent.change(quantity, { target: { value: "25" } });
    fireEvent.change(notes, { target: { value: "Test batch" } });
    fireEvent.click(screen.getByRole("button", { name: "Create batch" }));

    await screen.findByText("Save failed.");
    expect(recipe.value).toBe("recipe-1");
    expect(quantity.value).toBe("25");
    expect(notes.value).toBe("Test batch");
  });
});
