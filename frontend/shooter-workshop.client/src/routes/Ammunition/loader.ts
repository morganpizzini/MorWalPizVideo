import { getWorkshopBatches, getWorkshopRecipes } from "@morwalpizvideo/services";
import type { ShouldRevalidateFunctionArgs } from "react-router";

export interface AmmunitionLoaderData {
  recipes: Awaited<ReturnType<typeof getWorkshopRecipes>>;
  batches: Awaited<ReturnType<typeof getWorkshopBatches>>;
}

export interface AmmunitionLoaderResources {
  recipes: ReturnType<typeof getWorkshopRecipes>;
  batches: ReturnType<typeof getWorkshopBatches>;
  productionPanel: Promise<[
    Awaited<ReturnType<typeof getWorkshopRecipes>>,
    Awaited<ReturnType<typeof getWorkshopBatches>>,
  ]>;
}

export function loader() {
  const recipes = getWorkshopRecipes();
  const batches = getWorkshopBatches();
  return {
    recipes,
    batches,
    productionPanel: Promise.all([recipes, batches]),
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
