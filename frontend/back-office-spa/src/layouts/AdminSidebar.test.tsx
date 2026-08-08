import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen } from '@testing-library/react';
import { render } from '../test/test-utils';
import { authService } from '../services/authService';
import AdminSidebar from './AdminSidebar';

vi.mock('../services/authService', () => ({
  authService: {
    getPermissions: vi.fn(),
  },
}));

describe('AdminSidebar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(authService.getPermissions).mockReturnValue(['backoffice.access', 'videos.view']);
  });

  it('renders one permission-filtered navigation tree with active and close behavior', () => {
    const onHide = vi.fn();

    render(<AdminSidebar show onHide={onHide} />);

    expect(screen.getByRole('navigation')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveClass('active');
    expect(screen.getByRole('link', { name: 'Videos' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Channels' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Close' }));
    expect(onHide).toHaveBeenCalledOnce();

    fireEvent.click(screen.getByRole('link', { name: 'Dashboard' }));
    expect(onHide).toHaveBeenCalledTimes(2);
  });

  it('shows resource navigation for manage permission and routed catalog modules', () => {
    vi.mocked(authService.getPermissions).mockReturnValue([
      'videos.manage',
      'productcategories.manage',
      'sponsors.manage',
      'products.manage',
      'compilations.manage',
    ]);

    render(<AdminSidebar show onHide={vi.fn()} />);

    expect(screen.getByRole('link', { name: 'Videos' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Product categories' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sponsors' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Products' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Compilations' })).toBeInTheDocument();
  });
});
