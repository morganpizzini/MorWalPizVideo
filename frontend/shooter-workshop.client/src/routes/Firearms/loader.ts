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

export interface EquipmentLoaderResources {
  firearms: ReturnType<typeof getWorkshopFirearms>;
  definitions: ReturnType<typeof getWorkshopComponentDefinitions>;
  inventory: ReturnType<typeof getWorkshopComponents>;
  installations: ReturnType<typeof getWorkshopComponentInstallations>;
}

export function loader() {
  return deferRouteData<EquipmentLoaderResources>({
    firearms: getWorkshopFirearms(),
    definitions: getWorkshopComponentDefinitions(),
    inventory: getWorkshopComponents(),
    installations: getWorkshopComponentInstallations(),
  });
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
