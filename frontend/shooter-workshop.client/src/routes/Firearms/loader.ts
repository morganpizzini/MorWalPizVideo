import {
  getWorkshopComponentDefinitions,
  getWorkshopComponentInstallations,
  getWorkshopComponents,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";
import type { ShouldRevalidateFunctionArgs } from "react-router";
import { loadOfflineAware } from "../../services/offlineData";

export interface EquipmentLoaderData {
  firearms: Awaited<ReturnType<typeof getWorkshopFirearms>>;
  definitions: Awaited<ReturnType<typeof getWorkshopComponentDefinitions>>;
  inventory: Awaited<ReturnType<typeof getWorkshopComponents>>;
  installations: Awaited<ReturnType<typeof getWorkshopComponentInstallations>>;
}

export interface EquipmentLoaderResources {
  firearms: ReturnType<typeof getWorkshopFirearms>;
  definitions: ReturnType<typeof getWorkshopComponentDefinitions>;
  inventory: ReturnType<typeof getWorkshopComponents>;
  installations: ReturnType<typeof getWorkshopComponentInstallations>;
  inventoryPanel: Promise<[
    Awaited<ReturnType<typeof getWorkshopComponents>>,
    Awaited<ReturnType<typeof getWorkshopComponentDefinitions>>,
    Awaited<ReturnType<typeof getWorkshopFirearms>>,
    Awaited<ReturnType<typeof getWorkshopComponentInstallations>>,
  ]>;
}

export function loader() {
  const firearms = loadOfflineAware("firearms", getWorkshopFirearms);
  const definitions = loadOfflineAware("definitions", getWorkshopComponentDefinitions);
  const inventory = loadOfflineAware("components", getWorkshopComponents);
  const installations = loadOfflineAware("installations", getWorkshopComponentInstallations);
  return {
    firearms,
    definitions,
    inventory,
    installations,
    inventoryPanel: Promise.all([inventory, definitions, firearms, installations]),
  } satisfies EquipmentLoaderResources;
}

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
