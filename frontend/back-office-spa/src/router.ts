import { createBrowserRouter, redirect } from 'react-router';
import { endpoints, get, selectFirstAccessibleChannel } from '@morwalpizvideo/services';
import type { Channel } from './models/channel';
import { requireChannelPayload } from './routes/channels/response';
import PrimaryLayout from './layouts/PrimaryLayout';
import { authRoutes } from './router/routes/auth.routes';
import { protectedRoutes } from './router/routes';
import { requirePermissions } from './router/guards';
import { permissions } from './authorization/permissions';
import { authService } from './services/authService';
import ForbiddenPage from './routes/forbidden/Component';
import { useAppStore } from './state/appStore';

interface FeatureStateResponse {
  videoBulkImportEnabled: boolean;
}

export async function authLoader() {
  const session = await authService.validateSession();
  if (!session) {
    localStorage.removeItem('auth_user');
    return redirect('/login');
  }

  const permissionResult = await requirePermissions([permissions.backoffice.access], session);
  if (permissionResult instanceof Response) {
    return permissionResult;
  }

  const [channelResponse, featureResponse] = await Promise.all([
    get(endpoints.CHANNELS_ACCESSIBLE),
    get('/api/features') as Promise<FeatureStateResponse>,
  ]);
  const response = channelResponse;
  const channels = requireChannelPayload<unknown>(response, 'Unable to load accessible channels');
  if (!Array.isArray(channels)) {
    throw new Response('Unable to load accessible channels', { status: 502 });
  }

  const accessibleChannels = channels as Channel[];
  const selectedChannelId = selectFirstAccessibleChannel(accessibleChannels);
  useAppStore.getState().hydrate({
    user: authService.getUser(),
    effectivePermissions: session.effectivePermissions,
    featureFlags: { videoBulkImportEnabled: featureResponse.videoBulkImportEnabled },
    accessibleChannels,
    selectedChannelId,
    sessionStatus: 'authenticated',
  });
  return { session, channels: accessibleChannels, selectedChannelId };
}

/**
 * Application router configuration
 * 
 * Structure:
 * - Auth routes (login) - public routes
 * - Protected routes - require authentication, wrapped in PrimaryLayout
 * 
 * Route modules are organized in ./router/routes/
 */
export default createBrowserRouter([
  ...authRoutes,
  {
    path: '/forbidden',
    Component: ForbiddenPage,
    loader: async () => (await authService.validateSession()) ? null : redirect('/login'),
  },
  {
    path: '/',
    Component: PrimaryLayout,
    loader: authLoader,
    children: protectedRoutes,
  },
]);
