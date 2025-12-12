import Login from '../../routes/login';
import { createErrorElement } from '../utils';
import type { RouteConfig } from '../types';

export const authRoutes: RouteConfig[] = [
  {
    path: '/login',
    Component: Login.Component,
    action: Login.action,
    errorElement: createErrorElement(),
  },
];
