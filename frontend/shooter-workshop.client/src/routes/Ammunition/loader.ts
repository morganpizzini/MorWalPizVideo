import {
  getWorkshopBatches,
  getWorkshopProductionRecords,
  getWorkshopRecipeJournalEntries,
  getWorkshopRecipes,
} from "@morwalpizvideo/services";
import type { WorkshopRecipeJournalEntry } from "@morwalpizvideo/models";
import { loadOnlineInventory } from "../onlineInventory";
import type { OnlineInventoryAvailability } from "../onlineInventory";
import type { LoaderFunctionArgs, ShouldRevalidateFunctionArgs } from "react-router";

export interface AmmunitionLoaderData {
  recipes: Awaited<ReturnType<typeof getWorkshopRecipes>>;
  batches: Awaited<ReturnType<typeof getWorkshopBatches>>;
}

export interface RecipeJournalLoaderData {
  recipeId: string;
  entries: WorkshopRecipeJournalEntry[];
}

export interface AmmunitionLoaderResources {
  recipes: ReturnType<typeof getWorkshopRecipes>;
  batches: ReturnType<typeof getWorkshopBatches>;
  recipeJournal: Promise<RecipeJournalLoaderData | null>;
  inventory: Promise<OnlineInventoryAvailability>;
  productionRecords: ReturnType<typeof getWorkshopProductionRecords>;
  productionPanel: Promise<[
    Awaited<ReturnType<typeof getWorkshopRecipes>>,
    Awaited<ReturnType<typeof getWorkshopBatches>>,
    Awaited<ReturnType<typeof getWorkshopProductionRecords>>,
  ]>;
}

export function loader({ request }: LoaderFunctionArgs) {
  const recipes = getWorkshopRecipes();
  const batches = getWorkshopBatches();
  const inventory = loadOnlineInventory();
  const productionRecords = getWorkshopProductionRecords();
  const selectedRecipeId = new URL(request.url).searchParams.get("recipeId");
  const recipeJournal = selectedRecipeId
    ? recipes.then((loadedRecipes) =>
        loadedRecipes.some((recipe) => recipe.id === selectedRecipeId)
          ? getWorkshopRecipeJournalEntries(selectedRecipeId).then((entries) => ({
              recipeId: selectedRecipeId,
              entries,
            }))
          : null,
      )
    : Promise.resolve(null);
  return {
    recipes,
    batches,
    recipeJournal,
    inventory,
    productionRecords,
    productionPanel: Promise.all([recipes, batches, productionRecords]),
  } satisfies AmmunitionLoaderResources;
}

export function ammunitionShouldRevalidate({
  currentUrl,
  nextUrl,
  defaultShouldRevalidate,
}: ShouldRevalidateFunctionArgs): boolean {
  if (
    defaultShouldRevalidate &&
    currentUrl.pathname === nextUrl.pathname &&
    currentUrl.search !== nextUrl.search
  ) {
    const currentSearch = new URLSearchParams(currentUrl.search);
    const nextSearch = new URLSearchParams(nextUrl.search);
    currentSearch.delete("tab");
    nextSearch.delete("tab");
    if (currentSearch.toString() === nextSearch.toString()) return false;
  }

  return defaultShouldRevalidate;
}
