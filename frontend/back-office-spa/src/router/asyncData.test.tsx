import { render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider, useAsyncValue } from 'react-router';
import { describe, expect, it } from 'vitest';
import { createAsyncRouteComponent, deferRouteData } from './asyncData';

function renderRoute(loader: () => unknown, Component: React.ComponentType) {
  const router = createMemoryRouter(
    [{ path: '/', loader, Component: createAsyncRouteComponent(Component) }],
    { initialEntries: ['/'] }
  );
  return render(<RouterProvider router={router} />);
}

describe('async route data boundary', () => {
  it('renders the route component after loader data resolves', async () => {
    function RouteComponent() {
      return <div>{useAsyncValue() as string}</div>;
    }

    renderRoute(() => deferRouteData(Promise.resolve('resolved')), RouteComponent);

    await waitFor(() => expect(screen.getByText('resolved')).toBeInTheDocument());
  });

  it('renders a pending state while route data is unresolved', () => {
    const pending = new Promise<unknown>(() => undefined);

    renderRoute(
      () => deferRouteData(pending),
      () => <div>ready</div>
    );

    expect(screen.queryByText('ready')).not.toBeInTheDocument();
    return waitFor(() => {
      const status = screen.getByRole('status');
      expect(status).toHaveAttribute('aria-live', 'polite');
      expect(status).toHaveTextContent('Page content is loading');
      expect(status).not.toHaveTextContent('Loading page...');
    });
  });

  it('renders the shared error state when route data rejects', async () => {
    const rejected = Promise.reject(new Error('Request failed'));
    rejected.catch(() => undefined);
    renderRoute(
      () => deferRouteData(rejected),
      () => <div>ready</div>
    );

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Request failed'));
  });
});
