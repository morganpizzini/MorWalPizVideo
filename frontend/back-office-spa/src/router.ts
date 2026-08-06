import { createBrowserRouter, redirect } from 'react-router';
import PrimaryLayout from './layouts/PrimaryLayout';
import { authRoutes } from './router/routes/auth.routes';
import { protectedRoutes } from './router/routes';
import { authService } from './services/authService';

async function authLoader() {
  const session = await authService.validateSession();
  if (!session) {
    localStorage.removeItem('auth_user');
    return redirect('/login');
  }
  return null;
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
    path: '/',
    Component: PrimaryLayout,
    loader: authLoader,
    children: protectedRoutes,
  },
]);
