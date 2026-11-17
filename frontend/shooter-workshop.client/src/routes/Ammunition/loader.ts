import { getWorkshopBatches, getWorkshopRecipes } from "@morwalpizvideo/services";
import type { ShouldRevalidateFunctionArgs } from "react-router";

export async function loader() {
  const [recipes, batches] = await Promise.all([
    getWorkshopRecipes(),
    getWorkshopBatches(),
  ]);
  return { recipes, batches };
}

export type AmmunitionLoaderData = Awaited<ReturnType<typeof loader>>;

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
