import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render as rtlRender, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { useAppStore } from '../state/appStore';
import AdminSidebar from './AdminSidebar';

function renderAtPath(pathname: string, onHide = vi.fn()) {
  const router = createMemoryRouter(
    [{ path: '*', element: <AdminSidebar show onHide={onHide} /> }],
    { initialEntries: [pathname] }
  );
  return rtlRender(<RouterProvider router={router} />);
}

describe('AdminSidebar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAppStore.getState().reset();
    useAppStore.setState({
      effectivePermissions: [
        'backoffice.access',
        'videos.view',
        'products.view',
        'productcategories.view',
        'sponsors.view',
      ],
    });
  });

  it('keeps Dashboard visible and opens Publishing by default on the dashboard', () => {
    const onHide = vi.fn();

    renderAtPath('/', onHide);

    expect(screen.getByRole('navigation')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveClass('active');
    expect(screen.getByRole('link', { name: 'Videos' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Channels' })).not.toBeInTheDocument();
    const publishing = screen.getByRole('button', { name: 'Publishing' });
    expect(publishing).toHaveAttribute('aria-expanded', 'true');
    expect(publishing).toHaveAttribute('aria-expanded', 'true');

    fireEvent.click(screen.getByRole('button', { name: 'Close' }));
    expect(onHide).toHaveBeenCalledOnce();
  });

  it('allows one group at a time to expand and keeps the active group manually collapsible', () => {
    const onHide = vi.fn();
    renderAtPath('/videos', onHide);

    const publishing = screen.getByRole('button', { name: 'Publishing' });
    expect(publishing).toHaveAttribute('aria-current', 'location');
    fireEvent.click(publishing);
    expect(publishing).toHaveAttribute('aria-expanded', 'false');
    expect(screen.queryByRole('link', { name: 'Videos' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Catalog' }));
    expect(screen.getByRole('link', { name: 'Products' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Catalog' })).toHaveAttribute(
      'aria-expanded',
      'true'
    );
    expect(screen.getByRole('button', { name: 'Publishing' })).toHaveAttribute(
      'aria-expanded',
      'false'
    );
    expect(onHide).not.toHaveBeenCalled();
  });

  it('shows resource navigation for manage permission and routed catalog modules', () => {
    useAppStore.setState({
      effectivePermissions: [
        'videos.manage',
        'productcategories.manage',
        'sponsors.manage',
        'products.manage',
        'compilations.manage',
      ],
    });

    renderAtPath('/');

    expect(screen.getByRole('link', { name: 'Videos' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Catalog' }));
    expect(screen.getByRole('link', { name: 'Product categories' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sponsors' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Products' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Publishing' }));
    expect(screen.getByRole('link', { name: 'Videos' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Compilations' })).toBeInTheDocument();
  });

  it('shows Channels only with the dedicated administration permission', () => {
    useAppStore.setState({ effectivePermissions: ['channels.manage'] });
    renderAtPath('/');
    expect(screen.queryByRole('link', { name: 'Channels' })).not.toBeInTheDocument();

    act(() => useAppStore.setState({ effectivePermissions: ['channels.admin'] }));
    fireEvent.click(screen.getByRole('button', { name: 'Administration' }));
    expect(screen.getByRole('link', { name: 'Channels' })).toBeInTheDocument();
  });
});
