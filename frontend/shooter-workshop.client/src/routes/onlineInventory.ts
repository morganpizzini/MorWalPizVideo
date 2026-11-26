import { getWorkshopInventory } from "@morwalpizvideo/services";
import type { WorkshopInventory } from "@morwalpizvideo/models";

export interface OnlineInventoryAvailability {
  inventory: WorkshopInventory | null;
  online: boolean;
  error?: string;
}

export function loadOnlineInventory(): Promise<OnlineInventoryAvailability> {
  if (typeof navigator !== "undefined" && !navigator.onLine) {
    return Promise.resolve({ inventory: null, online: false });
  }
  return getWorkshopInventory().then(
    (inventory) => ({ inventory, online: true }),
    (error: unknown) => ({
      inventory: null,
      online: true,
      error: error instanceof Error ? error.message : "The inventory request could not be completed.",
    }),
  );
}
