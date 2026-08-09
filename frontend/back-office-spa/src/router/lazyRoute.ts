import type { ComponentType } from 'react';
import type { ActionFunction, LoaderFunction, RouteObject } from 'react-router';

interface RouteModule {
  [key: string]: unknown;
  default?: unknown;
  Component?: ComponentType;
  component?: ComponentType;
  loader?: LoaderFunction;
  Loader?: LoaderFunction;
  action?: ActionFunction;
  Action?: ActionFunction;
}

export function normalizeRouteModule(module: unknown): Omit<RouteObject, 'lazy' | 'children'> {
  const record = (typeof module === 'object' && module !== null ? module : {}) as RouteModule;
  const defaultExport = record.default;
  if (typeof defaultExport === 'function') {
    return { Component: defaultExport as ComponentType };
  }
  const source = (typeof record.default === 'object' && record.default !== null ? record.default : {}) as RouteModule;
  return {
    Component: record.Component ?? record.component ?? source.Component ?? source.component,
    loader: record.loader ?? record.Loader ?? source.loader ?? source.Loader,
    action: record.action ?? record.Action ?? source.action ?? source.Action,
  };
}

export function lazyRoute(importer: () => Promise<unknown>): Pick<RouteObject, 'lazy'> {
  return {
    lazy: async () => normalizeRouteModule(await importer()) as never,
  };
}

export function lazyAction(importer: () => Promise<unknown>): Pick<RouteObject, 'lazy'> {
  return {
    lazy: async () => {
      const module = await importer();
      const normalized = normalizeRouteModule(module);
      const defaultExport = typeof module === 'object' && module !== null ? (module as RouteModule).default : undefined;
      return { action: normalized.action ?? (typeof defaultExport === 'function' ? defaultExport as ActionFunction : undefined) } as never;
    },
  };
}
