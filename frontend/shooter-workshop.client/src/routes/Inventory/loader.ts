import {
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopInventory,
} from "@morwalpizvideo/services";
import type { WorkshopInventory } from "@morwalpizvideo/models";

export type InventoryRouteData = [
  WorkshopInventory,
  Awaited<ReturnType<typeof getWorkshopComponents>>,
  Awaited<ReturnType<typeof getWorkshopComponentDefinitions>>,
] | null;

export interface InventoryLoaderData {
  online: boolean;
  data: Promise<InventoryRouteData>;
}

export function loader(): InventoryLoaderData {
  const online = typeof navigator === "undefined" || navigator.onLine;
  if (!online) return { online: false, data: Promise.resolve(null) };
  return {
    online: true,
    data: Promise.all([
      getWorkshopInventory(),
      getWorkshopComponents(),
      getWorkshopComponentDefinitions(),
    ]),
  };
}
