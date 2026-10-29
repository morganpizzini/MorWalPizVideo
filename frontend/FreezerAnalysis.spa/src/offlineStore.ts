import type { FieldEventInput, Snapshot } from './freezerApi';

interface StoredSnapshot {
  key: string;
  accountId: string;
  snapshot: Snapshot;
}

interface QueuedFieldEvent {
  key: string;
  accountId: string;
  snapshotId: string;
  event: FieldEventInput;
}

const databaseName = 'freezer-analysis-offline';
const snapshotsStore = 'snapshots';
const eventsStore = 'queued-field-events';

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const opening = indexedDB.open(databaseName, 2);
    opening.onupgradeneeded = () => {
      const database = opening.result;
      const transaction = opening.transaction;
      if (!transaction) throw new Error('Offline storage upgrade transaction is unavailable.');
      const snapshotStore = database.objectStoreNames.contains(snapshotsStore)
        ? transaction.objectStore(snapshotsStore)
        : database.createObjectStore(snapshotsStore, { keyPath: 'key' });
      const eventStore = database.objectStoreNames.contains(eventsStore)
        ? transaction.objectStore(eventsStore)
        : database.createObjectStore(eventsStore, { keyPath: 'key' });
      if (!snapshotStore.indexNames.contains('accountId')) snapshotStore.createIndex('accountId', 'accountId');
      if (!eventStore.indexNames.contains('accountId')) eventStore.createIndex('accountId', 'accountId');
    };
    opening.onsuccess = () => resolve(opening.result);
    opening.onerror = () => reject(opening.error ?? new Error('Offline storage could not be opened.'));
    opening.onblocked = () => reject(new Error('Offline storage upgrade is blocked by another tab.'));
  });
}

function transact<T>(
  storeName: string,
  mode: IDBTransactionMode,
  operation: (store: IDBObjectStore) => IDBRequest<T>
): Promise<T> {
  return openDatabase().then(database => new Promise<T>((resolve, reject) => {
    let transaction: IDBTransaction;
    try {
      transaction = database.transaction(storeName, mode);
    } catch (error) {
      database.close();
      reject(error);
      return;
    }

    let result: { value: T } | undefined;
    let settled = false;
    const close = () => database.close();
    const fail = (error: DOMException | Error) => {
      if (settled) return;
      settled = true;
      close();
      reject(error);
    };

    let request: IDBRequest<T>;
    try {
      request = operation(transaction.objectStore(storeName));
    } catch (error) {
      transaction.abort();
      fail(error instanceof Error ? error : new Error('Offline storage operation failed.'));
      return;
    }

    request.onsuccess = () => {
      result = { value: request.result };
    };
    request.onerror = () => fail(request.error ?? new Error('Offline storage operation failed.'));
    transaction.onabort = () =>
      fail(transaction.error ?? new Error('Offline storage transaction was aborted.'));
    transaction.onerror = () =>
      fail(transaction.error ?? new Error('Offline storage transaction failed.'));
    transaction.oncomplete = () => {
      close();
      if (settled) return;
      settled = true;
      if (!result) {
        reject(new Error('Offline storage transaction completed without a result.'));
        return;
      }
      resolve(result.value);
    };
  }));
}

export async function saveSnapshot(accountId: string, snapshot: Snapshot): Promise<void> {
  await transact(snapshotsStore, 'readwrite', store => store.put({
    key: `${accountId}:${snapshot.id}`,
    accountId,
    snapshot
  } satisfies StoredSnapshot));
}

export async function getSnapshot(accountId: string, snapshotId: string): Promise<Snapshot | null> {
  const record = await transact<StoredSnapshot | undefined>(
    snapshotsStore,
    'readonly',
    store => store.get(`${accountId}:${snapshotId}`)
  );
  return record?.accountId === accountId ? record.snapshot : null;
}

export async function enqueueFieldEvent(
  accountId: string,
  snapshotId: string,
  event: FieldEventInput
): Promise<void> {
  const record: QueuedFieldEvent = {
    key: `${accountId}:${event.clientOperationId}`,
    accountId,
    snapshotId,
    event
  };
  await transact(eventsStore, 'readwrite', store => store.put(record));
}

export async function queuedFieldEvents(accountId: string): Promise<QueuedFieldEvent[]> {
  const records = await transact<QueuedFieldEvent[]>(eventsStore, 'readonly', store => store.getAll());
  return records.filter(record => record.accountId === accountId);
}

export async function removeQueuedFieldEvent(accountId: string, operationId: string): Promise<void> {
  await transact(eventsStore, 'readwrite', store => store.delete(`${accountId}:${operationId}`));
}

export async function clearAccountOfflineData(accountId: string): Promise<void> {
  if (!accountId.trim()) throw new Error('A valid account is required to clear offline data.');
  const database = await openDatabase();
  await new Promise<void>((resolve, reject) => {
    let settled = false;
    const close = () => database.close();
    const fail = (error: DOMException | Error) => {
      if (settled) return;
      settled = true;
      close();
      reject(error);
    };

    let transaction: IDBTransaction;
    try {
      transaction = database.transaction([snapshotsStore, eventsStore], 'readwrite');
    } catch (error) {
      close();
      reject(error instanceof Error ? error : new Error('Offline data could not be cleared.'));
      return;
    }

    for (const storeName of [snapshotsStore, eventsStore]) {
      const request = transaction.objectStore(storeName)
        .index('accountId')
        .openCursor(IDBKeyRange.only(accountId));
      request.onsuccess = () => {
        const cursor = request.result;
        if (!cursor) return;
        cursor.delete();
        cursor.continue();
      };
      request.onerror = () => fail(request.error ?? new Error('Offline data could not be cleared.'));
    }
    transaction.onabort = () =>
      fail(transaction.error ?? new Error('Offline data clearing was aborted.'));
    transaction.onerror = () =>
      fail(transaction.error ?? new Error('Offline data clearing failed.'));
    transaction.oncomplete = () => {
      close();
      if (settled) return;
      settled = true;
      resolve();
    };
  });
}
