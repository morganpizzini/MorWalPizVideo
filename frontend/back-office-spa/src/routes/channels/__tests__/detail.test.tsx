import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, render, act } from '@testing-library/react';
import { useState } from 'react';
import { createMemoryRouter, RouterProvider, useLoaderData, useFetcher, useNavigate } from 'react-router';
import { Channel } from '@morwalpizvideo/models';
import { ToastProvider } from '@components/ToastNotification';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useLoaderData: vi.fn(),
    useFetcher: vi.fn(),
    useNavigate: vi.fn(),
  };
});

const mockChannel: Channel = {
  channelId: '1',
  channelName: 'MorWalPiz',
  yTChannelId: 'UC12345',
  mine: false,
};

const mockFetcher = {
  state: 'idle',
  data: undefined as unknown,
  submit: vi.fn(),
};

beforeEach(() => {
  vi.clearAllMocks();
  mockFetcher.data = undefined;
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useLoaderData).mockReturnValue(mockChannel);
  vi.mocked(useFetcher).mockReturnValue(mockFetcher as any);
});

async function renderComponent() {
  const { default: Component } = await import('../detail/Component');
  let refresh: () => void = () => undefined;
  const TestHarness = () => {
    const [, setRenderVersion] = useState(0);
    refresh = () => setRenderVersion(version => version + 1);
    return <Component />;
  };
  const router = createMemoryRouter(
    [{ path: '*', element: <ToastProvider><TestHarness /></ToastProvider> }],
    { initialEntries: ['/channels/1'] }
  );
  return {
    view: render(<RouterProvider router={router} />),
    refresh: () => act(() => refresh()),
  };
}

describe('Channel Detail', () => {
  it('renders delete API errors and keeps the confirmation modal open', async () => {
    const { refresh } = await renderComponent();

    fireEvent.click(screen.getByRole('button', { name: /elimina/i }));
    expect(screen.getByText('Confirm Delete')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('delete-modal-confirm'));
    expect(mockFetcher.submit).toHaveBeenCalledWith(
      { id: '1' },
      expect.objectContaining({ method: 'post' })
    );

    mockFetcher.data = {
      success: false,
      errors: { generics: ['Channel was not found'] },
    };
    refresh();

    expect(screen.getByText('Channel was not found')).toBeInTheDocument();
    expect(screen.getByText('Confirm Delete')).toBeInTheDocument();
  });
});