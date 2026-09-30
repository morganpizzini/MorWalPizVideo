import React, { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';

interface DeferredRouteProgressContextValue {
  deferredPending: boolean;
  track: (promise: Promise<unknown>) => () => void;
}

const DeferredRouteProgressContext = createContext<DeferredRouteProgressContextValue | null>(null);

export function DeferredRouteProgressProvider({ children }: React.PropsWithChildren) {
  const [pendingCount, setPendingCount] = useState(0);
  const activePromises = useRef(new Set<Promise<unknown>>());

  const track = useCallback((promise: Promise<unknown>) => {
    if (activePromises.current.has(promise)) return () => undefined;

    activePromises.current.add(promise);
    setPendingCount(activePromises.current.size);

    const finish = () => {
      if (!activePromises.current.delete(promise)) return;
      setPendingCount(activePromises.current.size);
    };

    promise.then(finish, finish);
    return finish;
  }, []);

  const value = useMemo(
    () => ({ deferredPending: pendingCount > 0, track }),
    [pendingCount, track]
  );

  return (
    <DeferredRouteProgressContext.Provider value={value}>
      {children}
    </DeferredRouteProgressContext.Provider>
  );
}

export function useDeferredRouteProgress(promise: Promise<unknown>): void {
  const context = useContext(DeferredRouteProgressContext);

  React.useEffect(() => {
    return context?.track(promise);
  }, [context, promise]);
}

export function useDeferredRoutePending(): boolean {
  return useContext(DeferredRouteProgressContext)?.deferredPending ?? false;
}
