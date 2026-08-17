import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, render, act } from '@testing-library/react';
import { useState } from 'react';
import { createMemoryRouter, RouterProvider, useLoaderData, useFetcher, useNavigate } from 'react-router';
import { Channel } from '@morwalpizvideo/models';
import { ToastProvider } from '@components/ToastNotification';

const { mockNavigate, mockSelectChannel } = vi.hoisted(() => ({
  mockNavigate: vi.fn(),
  mockSelectChannel: vi.fn(),
}));

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useLoaderData: vi.fn(),
    useFetcher: vi.fn(),
    useNavigate: vi.fn(),
  };
});

vi.mock('../../../contexts/ChannelContext', () => ({
  useChannelContext: () => ({ selectChannel: mockSelectChannel }),
}));

const mockChannel: Channel = {
  channelId: '1',
  channelName: 'MorWalPiz',
  yTChannelId: 'UC12345',
  mine: false,
  shortLinkUrl: 'https://morwalpiz.com/sl',
  socials: [
    { provider: 'instagram', handler: '@morwalpiz' },
    { provider: 'x', handler: 'morwalpiz' },
  ],
};

const mockFetcher = {
  state: 'idle',
  data: undefined as unknown,
  submit: vi.fn(),
};

beforeEach(() => {
  vi.clearAllMocks();
  mockFetcher.data = undefined;
  vi.mocked(useNavigate).mockReturnValue(mockNavigate);
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
  it('selects the channel before opening Pages or Navigation management', async () => {
    await renderComponent();

    fireEvent.click(screen.getByRole('button', { name: 'Pages' }));
    expect(mockSelectChannel).toHaveBeenCalledWith('1');
    expect(mockNavigate).toHaveBeenCalledWith('/pages');
    expect(mockSelectChannel.mock.invocationCallOrder[0]).toBeLessThan(mockNavigate.mock.invocationCallOrder[0]);

    mockSelectChannel.mockClear();
    mockNavigate.mockClear();

    fireEvent.click(screen.getByRole('button', { name: 'Navigation' }));
    expect(mockSelectChannel).toHaveBeenCalledWith('1');
    expect(mockNavigate).toHaveBeenCalledWith('/navigation');
    expect(mockSelectChannel.mock.invocationCallOrder[0]).toBeLessThan(mockNavigate.mock.invocationCallOrder[0]);
  });

  it('renders the short-link base and every social entry', async () => {
    await renderComponent();

    expect(screen.getByText('https://morwalpiz.com/sl')).toBeInTheDocument();
    expect(screen.getByText('instagram: @morwalpiz')).toBeInTheDocument();
    expect(screen.getByText('x: morwalpiz')).toBeInTheDocument();
  });

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