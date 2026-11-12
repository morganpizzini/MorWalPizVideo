import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render as rtlRender, screen, waitFor } from '@testing-library/react';
import { useFetcher, useNavigate } from 'react-router';
import { useResolvedLoaderData } from '@/router/asyncData';
import { ToastProvider } from '@components/ToastNotification';
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
    id: 'document-id',
    channelId: 'owner',
    channelName: 'Owner',
    yTChannelId: 'UC_OWNER',
    publicBaseUrl: 'https://public.example',
    googleAnalyticsUrl: 'https://analytics.example/property',
  });
});

describe('Channel public origin field', () => {
  it('hydrates edit and submits an explicit clear', () => {
    render(<Component />);
    expect(screen.getByLabelText(/YouTube Channel ID/)).toHaveValue('UC_OWNER');
    expect(screen.queryByLabelText(/YouTube Studio link/)).not.toBeInTheDocument();
    expect(screen.getByLabelText('Google Analytics link')).toHaveValue(
      'https://analytics.example/property'
    );
    const input = screen.getByLabelText('Public website base URL');
    expect(input).toHaveValue('https://public.example');
    expect(input).toHaveAccessibleDescription(/Optional public website origin/);
    fireEvent.change(input, { target: { value: '' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
    fireEvent.click(screen.getByTestId('edit-modal-confirm'));
    expect(submit).toHaveBeenCalledWith(
      expect.objectContaining({
        publicBaseUrl: '',
        yTChannelId: 'UC_OWNER',
        googleAnalyticsUrl: 'https://analytics.example/property',
      }),
      expect.anything()
    );
  });

  it('confirms an edited YouTube binding without changing tenant identity', () => {
    render(<Component />);
    fireEvent.change(screen.getByLabelText(/YouTube Channel ID/), {
      target: { value: 'UC_REPLACEMENT' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));

    expect(screen.getByText(/future YouTube scans only/)).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('edit-modal-confirm'));

    expect(submit).toHaveBeenCalledWith(
      expect.objectContaining({ yTChannelId: 'UC_REPLACEMENT' }),
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

  it('closes the confirmation after failure, retains values, and focuses the invalid field', async () => {
    let response: unknown;
    vi.mocked(useFetcher).mockImplementation(
      () => ({ state: 'idle', submit, data: response } as never)
    );
    const navigate = vi.fn();
    vi.mocked(useNavigate).mockReturnValue(navigate);
    const view = rtlRender(
      <ToastProvider>
        <Component />
      </ToastProvider>
    );

    fireEvent.change(screen.getByLabelText('Google Analytics link'), {
      target: { value: 'https://analytics.example/updated' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
    fireEvent.click(screen.getByTestId('edit-modal-confirm'));

    response = {
      success: false,
      errors: { googleAnalyticsUrl: 'Analytics links must use HTTPS.' },
    };
    view.rerender(<ToastProvider><Component /></ToastProvider>);

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      expect(screen.getByLabelText('Google Analytics link')).toHaveFocus();
    });
    expect(screen.getByLabelText('Google Analytics link')).toHaveValue(
      'https://analytics.example/updated'
    );
    expect(navigate).not.toHaveBeenCalled();
  });
});
