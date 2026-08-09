import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import ProfilePage from './Component';
import { endpoints, get, put } from '@morwalpizvideo/services';
import { authService } from '../../services/authService';

vi.mock('@morwalpizvideo/services', () => ({
  endpoints: {
    USER_ME: 'api/user/me',
    USER_ME_PASSWORD: 'api/user/me/password',
  },
  get: vi.fn(),
  put: vi.fn(),
}));

vi.mock('../../services/authService', () => ({
  authService: {
    setUser: vi.fn(),
  },
}));

describe('Profile route', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(get).mockResolvedValue({
      id: 'u1',
      username: 'mario',
      email: 'mario@example.test',
    });
  });

  it('loads and shows profile data', async () => {
    render(<ProfilePage />);

    expect(await screen.findByDisplayValue('mario')).toBeInTheDocument();
    expect(screen.getByDisplayValue('mario@example.test')).toBeInTheDocument();
    expect(get).toHaveBeenCalledWith(endpoints.USER_ME);
  });

  it('updates personal data and shows success feedback', async () => {
    vi.mocked(put).mockResolvedValue({});
    render(<ProfilePage />);

    await screen.findByDisplayValue('mario');
    fireEvent.change(screen.getByLabelText('Username'), { target: { value: 'mario-updated' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'mario-updated@example.test' } });
    fireEvent.click(screen.getByRole('button', { name: /salva profilo/i }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(endpoints.USER_ME, {
        username: 'mario-updated',
        email: 'mario-updated@example.test',
        firstName: '',
        lastName: '',
        phone: '',
      });
    });
    expect(await screen.findByText('Profilo aggiornato con successo.')).toBeInTheDocument();
    expect(authService.setUser).toHaveBeenCalledOnce();
  });

  it('shows password update error when current password is invalid', async () => {
    vi.mocked(put).mockRejectedValueOnce(new Error('bad password'));
    render(<ProfilePage />);

    await screen.findByDisplayValue('mario');
    fireEvent.change(screen.getByLabelText('Password corrente'), { target: { value: 'wrong' } });
    fireEvent.change(screen.getByLabelText('Nuova password'), { target: { value: 'new-pass' } });
    fireEvent.click(screen.getByRole('button', { name: /aggiorna password/i }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(endpoints.USER_ME_PASSWORD, {
        currentPassword: 'wrong',
        newPassword: 'new-pass',
      });
    });
    expect(await screen.findByText(/aggiornamento password non riuscito/i)).toBeInTheDocument();
  });
});
