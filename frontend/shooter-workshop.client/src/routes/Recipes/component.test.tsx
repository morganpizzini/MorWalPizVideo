import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { transferableAbortController } from "node:util";
import { URLSearchParams as NodeURLSearchParams } from "node:url";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { WorkshopRecipe, WorkshopRecipeJournalEntry } from "@morwalpizvideo/models";
import { ApiResponseError } from "@morwalpizvideo/services";
import { createMemoryRouter, RouterProvider, useLocation } from "react-router";
import { I18nProvider } from "../../i18n";
import Recipes from "./component";

vi.mock("../../components/feedback/FeedbackProvider", () => ({
  useFeedback: () => ({ open: vi.fn() }),
}));

class NodeCompatibleAbortController {
  private readonly controller = transferableAbortController();

  get signal() {
    return this.controller.signal;
  }

  abort(reason?: unknown) {
    this.controller.abort(reason);
  }
}

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

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

beforeEach(() => {
  vi.stubGlobal("AbortController", NodeCompatibleAbortController);
  vi.stubGlobal("URLSearchParams", NodeURLSearchParams);
});

function SearchLocation() {
  const location = useLocation();
  return <output data-testid="search-location">{location.search}</output>;
}

describe("Recipes", () => {
  it("keeps recipe viewing available while online inventory is unavailable", () => {
    const router = createMemoryRouter([
      {
        path: "/ammunition",
        element: <Recipes recipes={recipes} inventory={null} embedded />,
      },
    ], { initialEntries: ["/ammunition"] });

    render(
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>,
    );

    expect(screen.getByRole("heading", { name: "Practice" })).toBeTruthy();
    expect((screen.getByRole("button", { name: "+ New recipe" }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText("Inventory must be available online to edit recipe products.")).toBeTruthy();
  });

  it("keeps the selected recipe in search parameters and displays its journal", async () => {
    const entry: WorkshopRecipeJournalEntry = {
      id: "journal-1",
      recipeId: "recipe-1",
      entryDate: "2026-11-25",
      timeOfDay: "Evening",
      weather: "Clear",
      temperature: "-2.50",
      temperatureUnit: "C",
      powerFactor: "123.40",
      notes: "range note",
      createdAt: "2026-11-25T10:00:00Z",
    };
    const router = createMemoryRouter([
      {
        path: "/ammunition",
        element: (
          <>
            <Recipes
              recipes={recipes}
              inventory={null}
              journal={Promise.resolve({ recipeId: "recipe-1", entries: [entry] })}
              embedded
            />
            <SearchLocation />
          </>
        ),
      },
    ], { initialEntries: ["/ammunition?tab=recipes&filter=active"] });

    render(
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>,
    );

    fireEvent.click(screen.getByRole("button", { name: /recipe details/i }));

    await waitFor(() => {
      const params = new URLSearchParams(screen.getByTestId("search-location").textContent ?? "");
      expect(params.get("recipeId")).toBe("recipe-1");
    });
    const params = new URLSearchParams(screen.getByTestId("search-location").textContent ?? "");
    expect(params.get("tab")).toBe("recipes");
    expect(params.get("filter")).toBe("active");

    const journal = await screen.findByRole("region", { name: "Recipe journal entries" });
    expect(within(journal).getByText("Clear")).toBeTruthy();
    expect(within(journal).getByText("-2.50 C")).toBeTruthy();
    expect(within(journal).getByText("123.40")).toBeTruthy();
    expect(within(journal).getByText("range note")).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    const closedParams = new URLSearchParams(screen.getByTestId("search-location").textContent ?? "");
    expect(closedParams.get("recipeId")).toBeNull();
    expect(closedParams.get("tab")).toBe("recipes");
    expect(closedParams.get("filter")).toBe("active");
  });

  it("keeps recipe details open and shows a journal load error", async () => {
    const journal = Promise.reject(new ApiResponseError({ status: 404, errors: ["Journal unavailable"] }));
    void journal.catch(() => {});
    const router = createMemoryRouter([
      {
        path: "/ammunition",
        element: <Recipes recipes={recipes} inventory={null} journal={journal} embedded />,
      },
    ], { initialEntries: ["/ammunition?tab=recipes"] });

    render(
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>,
    );

    fireEvent.click(screen.getByRole("button", { name: /recipe details/i }));
    const dialog = screen.getByRole("dialog", { name: "Practice" });
    expect((await within(dialog).findByRole("alert")).textContent).toContain("Journal unavailable");
    expect(within(dialog).getByText("Recipe journal")).toBeTruthy();
    expect(router.state.location.search).toContain("recipeId=recipe-1");
  });

  it("reuses the journal idempotency key and preserves the draft after an uncertain save", async () => {
    vi.stubGlobal("crypto", { randomUUID: () => "journal-retry-key" });
    const submittedKeys: string[] = [];
    const router = createMemoryRouter([
      {
        path: "/ammunition",
        action: async ({ request }) => {
          const form = await request.formData();
          submittedKeys.push(String(form.get("idempotencyKey")));
          return { ok: false, error: "Save outcome is uncertain.", intent: "journal-create" };
        },
        element: (
          <Recipes
            recipes={recipes}
            inventory={null}
            journal={Promise.resolve({ recipeId: "recipe-1", entries: [] })}
            embedded
          />
        ),
      },
    ], { initialEntries: ["/ammunition?tab=recipes&recipeId=recipe-1"] });

    render(
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Add journal entry" }));
    fireEvent.change(screen.getByLabelText("Entry date"), { target: { value: "2026-11-25" } });
    fireEvent.change(screen.getByLabelText("Weather"), { target: { value: "Clear" } });
    const saveButton = screen.getByRole("button", { name: "Save entry" });
    fireEvent.click(saveButton);
    await screen.findByRole("alert");
    fireEvent.click(saveButton);

    await waitFor(() => expect(submittedKeys).toHaveLength(2));
    expect(submittedKeys).toEqual(["journal-retry-key", "journal-retry-key"]);
    expect((screen.getByLabelText("Weather") as HTMLInputElement).value).toBe("Clear");
  });

  it("does not allow adding journal entries to archived recipes", () => {
    const archivedRecipe = { ...recipes[0], status: "Archived" as const };
    const router = createMemoryRouter([
      {
        path: "/ammunition",
        element: (
          <Recipes
            recipes={[archivedRecipe]}
            inventory={null}
            journal={Promise.resolve({ recipeId: "recipe-1", entries: [] })}
            embedded
          />
        ),
      },
    ], { initialEntries: ["/ammunition?recipeId=recipe-1"] });

    render(
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>,
    );

    expect((screen.getByRole("button", { name: "Add journal entry" }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText("Archived recipes are read-only.")).toBeTruthy();
  });
});
