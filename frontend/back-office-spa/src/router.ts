import { createBrowserRouter } from 'react-router';
import PrimaryLayout from './layouts/PrimaryLayout';
import { authRoutes } from './router/routes/auth.routes';
import { protectedRoutes } from './router/routes';

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
    children: protectedRoutes,
  },
]);
