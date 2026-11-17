import { getWorkshopBatches, getWorkshopRecipes } from "@morwalpizvideo/services";
import type { ShouldRevalidateFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export interface AmmunitionLoaderData {
  recipes: Awaited<ReturnType<typeof getWorkshopRecipes>>;
  batches: Awaited<ReturnType<typeof getWorkshopBatches>>;
}

export interface AmmunitionLoaderResources {
  recipes: ReturnType<typeof getWorkshopRecipes>;
  batches: ReturnType<typeof getWorkshopBatches>;
}

export function loader() {
  return deferRouteData<AmmunitionLoaderResources>({
    recipes: getWorkshopRecipes(),
    batches: getWorkshopBatches(),
  });
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
