import {
  getOfflineDataset,
  getRememberedAccountId,
  isNetworkFailure,
  saveOfflineDataset,
  type OfflineDataset,
} from "./offlineStore";

export async function loadOfflineAware<T>(
  dataset: OfflineDataset,
  load: () => Promise<T>,
): Promise<T> {
  const accountId = getRememberedAccountId();
  if (typeof navigator !== "undefined" && !navigator.onLine && accountId) {
    const cached = await getOfflineDataset<T>(accountId, dataset);
    if (cached !== null) return cached;
    throw new Error("This page is not saved for offline use. Reconnect and prepare this device first.");
  }

  try {
    const value = await load();
    if (!accountId) return value;
    try {
      await saveOfflineDataset(accountId, dataset, value);
      return await getOfflineDataset<T>(accountId, dataset) ?? value;
    } catch (storageError) {
      console.error("Workshop data could not be saved for offline use.", storageError);
      return value;
    }
  } catch (error) {
    if (!isNetworkFailure(error) || !accountId) throw error;
    const cached = await getOfflineDataset<T>(accountId, dataset);
    if (cached !== null) return cached;
    const detail = error instanceof Error ? ` ${error.message}` : "";
    throw new Error(`This page is not saved for offline use. Reconnect and prepare this device first.${detail}`);
  }
}
