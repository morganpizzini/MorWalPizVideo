import { createBrowserRouter, redirect } from 'react-router';
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

  return requirePermissions([permissions.backoffice.access], session);
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
