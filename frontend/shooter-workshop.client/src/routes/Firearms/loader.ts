import {
  getWorkshopComponentDefinitions,
  getWorkshopComponentInstallations,
  getWorkshopComponents,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";
import type { ShouldRevalidateFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export interface EquipmentLoaderData {
  firearms: Awaited<ReturnType<typeof getWorkshopFirearms>>;
  definitions: Awaited<ReturnType<typeof getWorkshopComponentDefinitions>>;
  inventory: Awaited<ReturnType<typeof getWorkshopComponents>>;
  installations: Awaited<ReturnType<typeof getWorkshopComponentInstallations>>;
}

export function loader() {
  return deferRouteData(Promise.all([
    getWorkshopFirearms(),
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
    getWorkshopComponentInstallations(),
  ]).then(([firearms, definitions, inventory, installations]): EquipmentLoaderData => ({
    firearms,
    definitions,
    inventory,
    installations,
  })));
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
