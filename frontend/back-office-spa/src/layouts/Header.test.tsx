import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { render } from '../test/test-utils';
import Header from './Header';
import { authService } from '../services/authService';
import { useLocation, useNavigate } from 'react-router';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useNavigate: vi.fn(),
    useLocation: vi.fn(),
  };
});

vi.mock('../services/authService', () => ({
  authService: {
    isAuthenticated: vi.fn(),
    getUser: vi.fn(),
    logout: vi.fn(),
  },
}));

describe('Header', () => {
  const navigateMock = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNavigate).mockReturnValue(navigateMock);
    vi.mocked(useLocation).mockReturnValue({ pathname: '/' } as any);
    vi.mocked(authService.isAuthenticated).mockReturnValue(true);
    vi.mocked(authService.getUser).mockReturnValue({
      id: 'user-1',
      username: 'tester',
      email: 'tester@example.test',
      role: 'viewer',
    } as any);
  });

  it('shows profile entry in user dropdown', async () => {
    render(<Header />);

    fireEvent.click(screen.getByRole('button', { name: /tester/i }));

    expect(await screen.findByRole('menuitem', { name: 'Profile' })).toBeInTheDocument();
  });

  it('keeps logout behavior intact', async () => {
    render(<Header />);

    fireEvent.click(screen.getByRole('button', { name: /tester/i }));
    fireEvent.click(await screen.findByRole('menuitem', { name: /logout/i }));

    await waitFor(() => {
      expect(authService.logout).toHaveBeenCalledOnce();
      expect(navigateMock).toHaveBeenCalledWith('/login');
    });
  });
});
