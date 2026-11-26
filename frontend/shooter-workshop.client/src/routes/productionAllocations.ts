import type {
  WorkshopInventory,
  WorkshopInventoryLot,
  WorkshopProductionLotAllocation,
  WorkshopProductionSession,
} from "@morwalpizvideo/models";

type AllocationKind = "primer" | "bullet";

export interface AllocatableLot {
  lot: WorkshopInventoryLot;
  availableQuantity: number;
}

function priorAllocations(
  session: WorkshopProductionSession | undefined,
  kind: AllocationKind,
): WorkshopProductionLotAllocation[] {
  if (!session) return [];
  return kind === "primer"
    ? session.primerLotAllocations
    : session.bulletLotAllocations;
}

export function allocateOldestLots(
  inventory: WorkshopInventory,
  productId: string,
  quantity: number,
  previous: WorkshopProductionLotAllocation[] = [],
): WorkshopProductionLotAllocation[] {
  if (!productId || !Number.isSafeInteger(quantity) || quantity <= 0) return [];

  let remaining = quantity;
  const allocations: WorkshopProductionLotAllocation[] = [];
  for (const { lot, availableQuantity } of getAllocatableLots(inventory, productId, previous)) {
    if (remaining === 0) break;
    const allocated = Math.min(availableQuantity, remaining);
    allocations.push({ lotId: lot.id, quantity: allocated });
    remaining -= allocated;
  }

  return remaining === 0 ? allocations : [];
}

export function getAllocatableLots(
  inventory: WorkshopInventory,
  productId: string,
  previous: WorkshopProductionLotAllocation[] = [],
): AllocatableLot[] {
  const restoredByLot = new Map<string, number>();
  for (const allocation of previous) {
    restoredByLot.set(
      allocation.lotId,
      (restoredByLot.get(allocation.lotId) ?? 0) + allocation.quantity,
    );
  }

  return inventory.lots
    .filter((lot) => lot.productId === productId)
    .map((lot) => ({
      lot,
      availableQuantity: lot.availableQuantity + (restoredByLot.get(lot.id) ?? 0),
    }))
    .filter(({ lot, availableQuantity }) =>
      availableQuantity > 0 && (!lot.isEmpty || restoredByLot.has(lot.id)),
    )
    .sort((left, right) =>
      left.lot.purchaseDate.localeCompare(right.lot.purchaseDate)
      || (left.lot.id < right.lot.id ? -1 : left.lot.id > right.lot.id ? 1 : 0),
    );
}

export function isValidOldestLotAllocation(
  inventory: WorkshopInventory,
  productId: string,
  quantity: number,
  allocations: WorkshopProductionLotAllocation[],
  session?: WorkshopProductionSession,
  kind: AllocationKind = "bullet",
): boolean {
  const previous = priorAllocations(session, kind);
  const expected = allocateOldestLots(inventory, productId, quantity, previous);
  return expected.length > 0
    && expected.length === allocations.length
    && expected.every((allocation, index) =>
      allocation.lotId === allocations[index]?.lotId
      && allocation.quantity === allocations[index]?.quantity,
    );
}
