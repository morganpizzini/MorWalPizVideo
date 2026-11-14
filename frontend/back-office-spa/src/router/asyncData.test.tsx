import { render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider, useAsyncValue } from 'react-router';
import { describe, expect, it } from 'vitest';
import { createAsyncRouteComponent, deferRouteData } from './asyncData';

function renderRoute(loader: () => unknown, Component: React.ComponentType) {
  const router = createMemoryRouter(
    [{ path: '/', loader, Component: createAsyncRouteComponent(Component) }],
    { initialEntries: ['/'] }
  );
  router.initialize();
  render(<RouterProvider router={router} />);
  return router;
}

describe('async route data boundary', () => {
  it('renders the route component after loader data resolves', async () => {
    function RouteComponent() {
      return <div>{useAsyncValue() as string}</div>;
    }

    const router = renderRoute(() => deferRouteData(Promise.resolve('resolved')), RouteComponent);
    try {
      await waitFor(() => expect(screen.getByText('resolved')).toBeInTheDocument());
    } finally {
      router.dispose();
    }
  });

  it('renders a pending state while route data is unresolved', async () => {
    const pending = new Promise<unknown>(() => undefined);

    const router = renderRoute(
      () => deferRouteData(pending),
      () => <div>ready</div>
    );

    try {
      expect(screen.queryByText('ready')).not.toBeInTheDocument();
      await waitFor(() => {
        const status = screen.getByRole('status');
        expect(status).toHaveAttribute('aria-live', 'polite');
        expect(status).toHaveTextContent('Page content is loading');
        expect(status).not.toHaveTextContent('Loading page...');
      });
    } finally {
      router.dispose();
    }
  });

  it('renders the shared error state when route data rejects', async () => {
    const rejected = Promise.reject(new Error('Request failed'));
    rejected.catch(() => undefined);
    const router = renderRoute(
      () => deferRouteData(rejected),
      () => <div>ready</div>
    );

    try {
      await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Request failed'));
    } finally {
      router.dispose();
    }
  });
});
