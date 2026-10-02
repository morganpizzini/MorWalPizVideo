import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor } from '../test/test-utils';
import PrimaryLayout from './PrimaryLayout';
import { useDeferredRouteProgress } from '../contexts/DeferredRouteProgressContext';

let resolveDeferred: () => void = () => undefined;
let deferredPromise: Promise<void> = new Promise(() => undefined);

function DeferredRouteContent() {
  useDeferredRouteProgress(deferredPromise);
  return <section>Route content</section>;
}

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    Outlet: () => <DeferredRouteContent />,
    useLoaderData: () => ({ channels: [] }),
  };
});

vi.mock('@components/Breadcrumbs', () => ({
  default: () => <nav aria-label="Breadcrumbs">Breadcrumbs</nav>,
}));

vi.mock('./Header', () => ({
  default: ({ onToggleSidebar }: { onToggleSidebar?: () => void }) => (
    <header>
      <button type="button" onClick={onToggleSidebar}>
        Toggle sidebar
      </button>
    </header>
  ),
}));

vi.mock('./AdminSidebar', () => ({
  default: ({ show }: { show?: boolean }) => (
    <aside data-open={show ? 'true' : 'false'}>Sidebar</aside>
  ),
}));

beforeEach(() => {
  deferredPromise = new Promise(() => undefined);
});

describe('PrimaryLayout', () => {
  it('keeps the protected shell composition and renders the footer after content', () => {
    const { container } = render(<PrimaryLayout />);

    const shell = container.querySelector('.admin-shell');
    const mainColumn = container.querySelector('.admin-main-layout');
    const content = container.querySelector('.admin-content');
    const footer = container.querySelector('.admin-footer');

    expect(shell).not.toBeNull();
    expect(mainColumn).not.toBeNull();
    expect(content).not.toBeNull();
    expect(footer).not.toBeNull();
    expect(mainColumn?.lastElementChild).toBe(footer);
    expect(content?.textContent).toContain('Breadcrumbs');
    expect(content?.textContent).toContain('Route content');
    expect(footer).not.toHaveClass('mt-5');
  });

  it('keeps the fixed progress bar active until deferred route data resolves', async () => {
    deferredPromise = new Promise<void>(resolve => {
      resolveDeferred = resolve;
    });

    const { container } = render(<PrimaryLayout />);
    const progress = container.querySelector('.router-progress');

    await waitFor(() => expect(progress).toHaveClass('is-active'));
    await act(async () => resolveDeferred());
    await waitFor(() => expect(progress).not.toHaveClass('is-active'));
    expect(screen.getByText('Route content')).toBeInTheDocument();
  });
});
