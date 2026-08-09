import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { render } from '../test/test-utils';
import Header from './Header';
import { authService } from '../services/authService';
import { useNavigate } from 'react-router';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useNavigate: vi.fn(),
  };
});

vi.mock('../services/authService', () => ({
  authService: {
    getUser: vi.fn(() => null),
    logout: vi.fn(),
  },
}));

describe('Header', () => {
  const navigateMock = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNavigate).mockReturnValue(navigateMock);
  });

  it('shows authenticated shell controls and a safe label without display identity', async () => {
    render(<Header />);

    expect(screen.getByRole('button', { name: 'Open navigation' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Notifications' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'User menu' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'User menu' }));
    expect(await screen.findByText('Authenticated user')).toBeInTheDocument();
    expect(navigateMock).not.toHaveBeenCalled();
  });

  it('shows profile entry in user dropdown', async () => {
    render(<Header />);

    expect(screen.queryByRole('button', { name: /toggle navigation/i })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Notifications' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'User menu' }));

    expect(await screen.findByRole('menuitem', { name: 'Profile' })).toBeInTheDocument();
  });

  it('keeps logout behavior intact', async () => {
    render(<Header />);

    fireEvent.click(screen.getByRole('button', { name: 'User menu' }));
    fireEvent.click(await screen.findByRole('menuitem', { name: /logout/i }));

    await waitFor(() => {
      expect(authService.logout).toHaveBeenCalledOnce();
      expect(navigateMock).toHaveBeenCalledWith('/login');
    });
  });
});
