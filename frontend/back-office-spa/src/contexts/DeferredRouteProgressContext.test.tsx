import { act, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import {
  DeferredRouteProgressProvider,
  useDeferredRoutePending,
  useDeferredRouteProgress,
} from './DeferredRouteProgressContext';

function ProgressProbe({ promise }: { promise: Promise<unknown> }) {
  useDeferredRouteProgress(promise);
  return <div data-testid="progress-state">{useDeferredRoutePending() ? 'active' : 'idle'}</div>;
}

describe('deferred route progress', () => {
  it('stays active until deferred data resolves and clears on unmount', async () => {
    let resolve: () => void = () => undefined;
    const pending = new Promise<void>(completion => {
      resolve = completion;
    });
    const view = render(
      <DeferredRouteProgressProvider>
        <ProgressProbe promise={pending} />
      </DeferredRouteProgressProvider>
    );

    expect(screen.getByTestId('progress-state')).toHaveTextContent('active');
    act(() => resolve());
    await waitFor(() => expect(screen.getByTestId('progress-state')).toHaveTextContent('idle'));

    let reject: (reason?: unknown) => void = () => undefined;
    const rejected = new Promise<void>((_, failure) => {
      reject = failure;
    });
    view.rerender(
      <DeferredRouteProgressProvider>
        <ProgressProbe promise={rejected} />
      </DeferredRouteProgressProvider>
    );
    expect(screen.getByTestId('progress-state')).toHaveTextContent('active');
    act(() => reject(new Error('failed')));
    await waitFor(() => expect(screen.getByTestId('progress-state')).toHaveTextContent('idle'));

    const abandoned = new Promise<void>(() => undefined);
    view.rerender(
      <DeferredRouteProgressProvider>
        <ProgressProbe promise={abandoned} />
      </DeferredRouteProgressProvider>
    );
    expect(screen.getByTestId('progress-state')).toHaveTextContent('active');
    view.unmount();
  });
});
