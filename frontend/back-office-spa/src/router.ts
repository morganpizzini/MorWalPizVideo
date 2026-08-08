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

  const response = await get(endpoints.CHANNELS);
  const channels = requireChannelPayload<unknown>(response, 'Unable to load accessible channels');
  if (!Array.isArray(channels)) {
    throw new Response('Unable to load accessible channels', { status: 502 });
  }

  const accessibleChannels = channels as Channel[];
  const selectedChannelId = selectFirstAccessibleChannel(accessibleChannels);
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
