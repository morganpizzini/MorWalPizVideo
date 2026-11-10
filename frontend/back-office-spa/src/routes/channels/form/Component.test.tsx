import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen } from '@testing-library/react';
import { useFetcher, useNavigate } from 'react-router';
import { useResolvedLoaderData } from '@/router/asyncData';
import { render } from '../../../test/test-utils';
import Component from './Component';

vi.mock('@/router/asyncData', () => ({ useResolvedLoaderData: vi.fn() }));
vi.mock('react-router', async () => ({
  ...(await vi.importActual<typeof import('react-router')>('react-router')),
  useFetcher: vi.fn(),
  useNavigate: vi.fn(),
}));

const submit = vi.fn();
beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useFetcher).mockReturnValue({ state: 'idle', submit } as never);
  vi.mocked(useResolvedLoaderData).mockReturnValue({
    channelId: 'owner',
    channelName: 'Owner',
    publicBaseUrl: 'https://public.example',
  });
});

describe('Channel public origin field', () => {
  it('hydrates edit and submits an explicit clear', () => {
    render(<Component />);
    const input = screen.getByLabelText('Public website base URL');
    expect(input).toHaveValue('https://public.example');
    expect(input).toHaveAccessibleDescription(/Optional public website origin/);
    fireEvent.change(input, { target: { value: '' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
    fireEvent.click(screen.getByTestId('edit-modal-confirm'));
    expect(submit).toHaveBeenCalledWith(
      expect.objectContaining({ publicBaseUrl: '' }),
      expect.anything()
    );
  });

  it('submits the public origin when creating a channel', () => {
    vi.mocked(useResolvedLoaderData).mockReturnValue(null);
    render(<Component />);
    fireEvent.change(screen.getByLabelText(/Channel Name/), { target: { value: 'New' } });
    fireEvent.change(screen.getByLabelText(/YouTube Channel ID/), { target: { value: 'UCNEW' } });
    fireEvent.change(screen.getByLabelText('Public website base URL'), {
      target: { value: 'https://new.example' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Create' }));
    fireEvent.click(screen.getByTestId('create-modal-confirm'));
    expect(submit).toHaveBeenCalledWith(
      expect.objectContaining({ publicBaseUrl: 'https://new.example' }),
      expect.anything()
    );
  });

  it('associates field errors and leaves API errors visible without navigation', () => {
    const navigate = vi.fn();
    vi.mocked(useNavigate).mockReturnValue(navigate);
    vi.mocked(useFetcher).mockReturnValue({
      state: 'idle',
      submit,
      data: {
        success: false,
        errors: {
          publicBaseUrl: 'Origin only',
          generics: ['Save failed'],
        },
      },
    } as never);
    render(<Component />);
    expect(screen.getByLabelText('Public website base URL')).toHaveAttribute(
      'aria-invalid',
      'true'
    );
    expect(screen.getByLabelText('Public website base URL')).toHaveAccessibleDescription(
      /Origin only/
    );
    expect(screen.getByText('Save failed')).toBeInTheDocument();
    expect(navigate).not.toHaveBeenCalled();
  });
});
