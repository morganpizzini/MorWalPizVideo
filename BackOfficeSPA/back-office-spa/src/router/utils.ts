import React from 'react';
import { Outlet } from 'react-router';
import ErrorBoundary from '../components/ErrorBoundary';
import type { RouteConfig } from './types';

/**
 * Creates a standard error element for routes
 */
export const createErrorElement = () => React.createElement(ErrorBoundary);

/**
 * Creates a route group with common configuration
 */
export const createRouteGroup = (
  path: string,
  options: {
    action?: RouteConfig['action'];
    children: RouteConfig[];
  }
): RouteConfig => ({
  path,
  Component: Outlet,
  action: options.action,
  errorElement: createErrorElement(),
  children: options.children,
});
