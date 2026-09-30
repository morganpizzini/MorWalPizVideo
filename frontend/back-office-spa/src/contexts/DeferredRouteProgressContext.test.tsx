import { act, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
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
  it('registers an unresolved promise once across provider state rerenders', async () => {
    let resolve: () => void = () => undefined;
    const pending = new Promise<void>(completion => {
      resolve = completion;
    });
    const trackPromise = vi.spyOn(pending, 'then');

    render(
      <DeferredRouteProgressProvider>
        <ProgressProbe promise={pending} />
      </DeferredRouteProgressProvider>
    );

    await waitFor(() => expect(trackPromise).toHaveBeenCalledTimes(1));
    expect(screen.getByTestId('progress-state')).toHaveTextContent('active');

    act(() => resolve());
    await waitFor(() => expect(screen.getByTestId('progress-state')).toHaveTextContent('idle'));
  });

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

    let resolveAbandoned: () => void = () => undefined;
    const abandoned = new Promise<void>(completion => {
      resolveAbandoned = completion;
    });
    let resolveReplacement: () => void = () => undefined;
    const replacement = new Promise<void>(completion => {
      resolveReplacement = completion;
    });
    view.rerender(
      <DeferredRouteProgressProvider>
        <ProgressProbe promise={abandoned} />
      </DeferredRouteProgressProvider>
    );
    expect(screen.getByTestId('progress-state')).toHaveTextContent('active');

    view.rerender(
      <DeferredRouteProgressProvider>
        <ProgressProbe promise={replacement} />
      </DeferredRouteProgressProvider>
    );
    expect(screen.getByTestId('progress-state')).toHaveTextContent('active');
    act(() => resolveAbandoned());
    expect(screen.getByTestId('progress-state')).toHaveTextContent('active');
    act(() => resolveReplacement());
    await waitFor(() => expect(screen.getByTestId('progress-state')).toHaveTextContent('idle'));

    view.unmount();
  });
});
