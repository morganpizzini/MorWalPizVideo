import 'fake-indexeddb/auto';
import { afterEach, describe, expect, it } from 'vitest';
import type { FieldEventInput, Snapshot } from './freezerApi';
import {
  clearAccountOfflineData,
  enqueueFieldEvent,
  getSnapshot,
  queuedFieldEvents,
  removeQueuedFieldEvent,
  saveSnapshot
} from './offlineStore';

afterEach(async () => {
  await new Promise<void>((resolve, reject) => {
    const request = indexedDB.deleteDatabase('freezer-analysis-offline');
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error);
    request.onblocked = () => reject(new Error('Offline test database deletion is blocked.'));
  });
});

describe('offline storage', () => {
  it('partitions snapshots and queued operations by account', async () => {
    const snapshot: Snapshot = {
      id: 'snapshot-1',
      matchId: 'match-1',
      engineVersion: 'engine-v1',
      parameterVersion: 'parameters-v1',
      createdAtUtc: '2026-10-29T10:00:00Z',
      stages: []
    };
    const event: FieldEventInput = {
      clientOperationId: 'operation-1',
      kind: 'reconnaissance',
      content: 'Synthetic note',
      observedAtLocal: '2026-10-29T12:00:00',
      observedOffsetMinutes: 120
    };

    await saveSnapshot('account-a', snapshot);
    await enqueueFieldEvent('account-a', snapshot.id, event);
    await enqueueFieldEvent('account-b', snapshot.id, event);

    expect(await getSnapshot('account-a', snapshot.id)).toEqual(snapshot);
    expect(await getSnapshot('account-b', snapshot.id)).toBeNull();
    expect(await queuedFieldEvents('account-a')).toEqual([
      expect.objectContaining({ accountId: 'account-a', snapshotId: snapshot.id, event })
    ]);
    expect(await queuedFieldEvents('account-b')).toEqual([
      expect.objectContaining({ accountId: 'account-b', snapshotId: snapshot.id, event })
    ]);

    await removeQueuedFieldEvent('account-a', event.clientOperationId);

    expect(await queuedFieldEvents('account-a')).toEqual([]);
    expect(await queuedFieldEvents('account-b')).toHaveLength(1);
  });

  it('clears only the signed-out account snapshots and queued operations', async () => {
    const snapshot: Snapshot = {
      id: 'snapshot-private',
      matchId: 'match-1',
      engineVersion: 'engine-v1',
      parameterVersion: 'parameters-v1',
      createdAtUtc: '2026-10-29T10:00:00Z',
      stages: []
    };
    const event: FieldEventInput = {
      clientOperationId: 'operation-private',
      kind: 'reconnaissance',
      content: 'Private queued note',
      observedAtLocal: '2026-10-29T12:00:00',
      observedOffsetMinutes: 120
    };
    await saveSnapshot('account-a', snapshot);
    await enqueueFieldEvent('account-a', snapshot.id, event);
    await saveSnapshot('account-b', snapshot);
    await enqueueFieldEvent('account-b', snapshot.id, event);

    await clearAccountOfflineData('account-a');

    expect(await getSnapshot('account-a', snapshot.id)).toBeNull();
    expect(await queuedFieldEvents('account-a')).toEqual([]);
    expect(await getSnapshot('account-b', snapshot.id)).toEqual(snapshot);
    expect(await queuedFieldEvents('account-b')).toHaveLength(1);
  });
});
