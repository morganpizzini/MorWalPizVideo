import React, { Suspense } from 'react';
import { Await, useAsyncError, useAsyncValue, useLoaderData } from 'react-router';

export interface DeferredRouteData<T> {
  data: Promise<T>;
}

export function deferRouteData<T>(data: T | Promise<T>): DeferredRouteData<T> {
  return { data: Promise.resolve(data) };
}

export function useResolvedLoaderData<T>(): T {
  const asyncValue = useAsyncValue();
  const loaderData = useLoaderData<T>();
  return (asyncValue === undefined ? loaderData : asyncValue) as T;
}

function AsyncRouteError() {
  const error = useAsyncError();
  const message = error instanceof Error ? error.message : 'Unable to load this page.';

  return (
    <div className="alert alert-danger" role="alert">
      {message}
    </div>
  );
}

interface AsyncRouteBoundaryProps {
  Component: React.ComponentType;
  loaderData: DeferredRouteData<unknown>;
}

export function AsyncRouteBoundary({ Component, loaderData }: AsyncRouteBoundaryProps) {
  return (
    <Suspense fallback={<AsyncRoutePending />}>
      <Await resolve={loaderData.data} errorElement={<AsyncRouteError />}>
        <Component />
      </Await>
    </Suspense>
  );
}

export function createAsyncRouteComponent(Component: React.ComponentType) {
  return function AsyncRouteComponent() {
    const loaderData = useLoaderData<DeferredRouteData<unknown>>();
    return <AsyncRouteBoundary Component={Component} loaderData={loaderData} />;
  };
}

function AsyncRoutePending() {
  return (
    <div role="status" aria-live="polite">
      Loading page...
    </div>
  );
}
