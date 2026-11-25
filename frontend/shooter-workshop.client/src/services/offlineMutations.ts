import {
  createOfflineCommand,
  enqueueOfflineCommand,
  getOfflineState,
  getRememberedAccountId,
  isNetworkFailure,
  type OfflineCommand,
} from "./offlineStore";

export async function runOrQueueOffline<T>(
  runOnline: () => Promise<T>,
  kind: OfflineCommand["kind"],
  payload: object,
  changes?: OfflineCommand["changes"],
  localIds?: Record<string, string>,
): Promise<{ offline: true } | { offline: false; value: T }> {
  const accountId = getRememberedAccountId();
  const queue = async () => {
    if (!accountId) throw new Error("Sign in online and prepare this device before recording offline changes.");
    if (!(await getOfflineState(accountId)).preparedAt) {
      throw new Error("Prepare this device while online before recording offline changes.");
    }
    await enqueueOfflineCommand(
      accountId,
      createOfflineCommand(kind, payload, changes, localIds),
    );
    return { offline: true as const };
  };

  if (typeof navigator !== "undefined" && !navigator.onLine) return queue();
  try {
    return { offline: false, value: await runOnline() };
  } catch (error) {
    if (!isNetworkFailure(error)) throw error;
    return queue();
  }
}
