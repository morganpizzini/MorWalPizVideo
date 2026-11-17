import {
  getWorkshopComponentDefinitions,
  getWorkshopComponentInstallations,
  getWorkshopComponents,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";
import type { ShouldRevalidateFunctionArgs } from "react-router";

export async function loader() {
  const [firearms, definitions, inventory, installations] = await Promise.all([
    getWorkshopFirearms(),
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
    getWorkshopComponentInstallations(),
  ]);
  return { firearms, definitions, inventory, installations };
}

export type FirearmsLoaderData = Awaited<ReturnType<typeof loader>>;
export type EquipmentLoaderData = FirearmsLoaderData;

export function firearmsShouldRevalidate({
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
    currentSearch.delete("firearmId");
    nextSearch.delete("tab");
    nextSearch.delete("firearmId");
    if (currentSearch.toString() === nextSearch.toString()) return false;
  }
  return defaultShouldRevalidate;
}
