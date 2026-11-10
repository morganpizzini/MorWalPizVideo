import { useEffect, useState } from 'react';
import { useLocation, useRouteLoaderData } from 'react-router';
import type { QuickActionsResponse } from '@morwalpizvideo/models';
import { getDashboardQuickActions } from '@morwalpizvideo/services';
import { useAppStore } from '../state/appStore';

type State =
  | { scope: string; status: 'loading' }
  | { scope: string; status: 'ready'; data: QuickActionsResponse }
  | { scope: string; status: 'error'; message: string };

export function useDashboardQuickActions() {
  const root = useRouteLoaderData<{ session: { userId: string } }>('backoffice');
  const channelId = useAppStore(state => state.selectedChannelId);
  const permissionKey = useAppStore(state => state.effectivePermissions).join('\n');
  const location = useLocation();
  const userId = root?.session.userId;
  const scope = JSON.stringify([userId, channelId, permissionKey]);
  const [retry, setRetry] = useState(0);
  const [state, setState] = useState<State>({ scope, status: 'loading' });

  useEffect(() => {
    if (!userId || !channelId) return;
    let active = true;
    setState({ scope, status: 'loading' });
    getDashboardQuickActions()
      .then(data => {
        if (data.channelId !== channelId) throw new Error('Canale della risposta non valido.');
        if (active) setState({ scope, status: 'ready', data });
      })
      .catch(() => {
        if (active)
          setState({ scope, status: 'error', message: 'Impossibile caricare le attività rapide.' });
      });
    return () => {
      active = false;
    };
  }, [scope, userId, channelId, root, retry, location.key]);

  const current: State = state.scope === scope ? state : { scope, status: 'loading' };
  return {
    scope,
    userId,
    channelId,
    available: Boolean(userId && channelId),
    state: current,
    reload: () => setRetry(value => value + 1),
  };
}
