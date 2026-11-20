import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { createContext, forwardRef, useContext, useState, type ComponentProps } from "react";
import type { useFetcher } from "react-router";
import { afterEach, describe, expect, it } from "vitest";
import { I18nProvider } from "../../i18n";
import type { action } from "./action";
import AmmunitionPanel from "./AmmunitionPanel";

type AmmunitionFetcher = ReturnType<typeof useFetcher<typeof action>>;

const SubmitFormContext = createContext<() => void>(() => {});

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

function ProductionFormHarness({ result }: { result: Awaited<ReturnType<typeof action>> }) {
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
          recipes={[
            {
              id: "recipe-1",
              accountId: "account-1",
              name: "Practice",
              caliber: "9x19",
              status: "Active",
              version: 1,
              updatedAt: "2026-11-15T00:00:00Z",
            },
          ]}
          batches={[]}
          fetcher={fetcher}
        />
      </SubmitFormContext.Provider>
    </I18nProvider>
  );
}

afterEach(cleanup);

describe("AmmunitionPanel", () => {
  it("resets the production form after a successful submission", async () => {
    render(<ProductionFormHarness result={{ ok: true }} />);

    const recipe = screen.getByLabelText("Recipe") as HTMLSelectElement;
    const quantity = screen.getByLabelText("Quantity") as HTMLInputElement;
    const productionDate = screen.getByLabelText("Date") as HTMLInputElement;
    const notes = screen.getByLabelText("Notes") as HTMLTextAreaElement;
    fireEvent.change(recipe, { target: { value: "recipe-1" } });
    fireEvent.change(quantity, { target: { value: "25" } });
    fireEvent.change(notes, { target: { value: "Test batch" } });
    fireEvent.click(screen.getByRole("button", { name: "Create batch" }));

    await screen.findByText("Production recorded.");
    await waitFor(() => {
      expect(recipe.value).toBe("");
      expect(quantity.value).toBe("0");
      expect(productionDate.value).toBe(productionDate.defaultValue);
      expect(notes.value).toBe("");
    });
  });

  it("keeps the entered values when submission fails", async () => {
    render(<ProductionFormHarness result={{ ok: false, error: "Save failed." }} />);

    const recipe = screen.getByLabelText("Recipe") as HTMLSelectElement;
    const quantity = screen.getByLabelText("Quantity") as HTMLInputElement;
    const notes = screen.getByLabelText("Notes") as HTMLTextAreaElement;
    fireEvent.change(recipe, { target: { value: "recipe-1" } });
    fireEvent.change(quantity, { target: { value: "25" } });
    fireEvent.change(notes, { target: { value: "Test batch" } });
    fireEvent.click(screen.getByRole("button", { name: "Create batch" }));

    await screen.findByText("Save failed.");
    expect(recipe.value).toBe("recipe-1");
    expect(quantity.value).toBe("25");
    expect(notes.value).toBe("Test batch");
  });
});
