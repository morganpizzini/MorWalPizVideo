import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import { useLoaderData, useFetcher, useNavigate, useSearchParams, useLocation } from 'react-router';
import { Channel } from '@morwalpizvideo/models';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useLoaderData: vi.fn(),
    useFetcher: vi.fn(),
    useNavigate: vi.fn(),
    useLocation: vi.fn(),
    useSearchParams: vi.fn(),
  };
});

const mockChannels: Channel[] = [
  { channelId: '1', channelName: 'MorWalPiz', yTChannelId: 'UC12345' },
  { channelId: '2', channelName: 'WalPiz Extra', yTChannelId: 'UC67890' },
];

const mockSubmit = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useLoaderData).mockReturnValue(mockChannels);
  vi.mocked(useFetcher).mockReturnValue({
    state: 'idle',
    data: undefined,
    submit: mockSubmit,
  } as any);
  vi.mocked(useLocation).mockReturnValue({
    pathname: '/channels',
    search: '',
    hash: '',
    state: null,
    key: 'default',
  });
  vi.mocked(useSearchParams).mockReturnValue([new URLSearchParams(), vi.fn()] as any);
});

async function renderComponent() {
  const { default: Component } = await import('../index/Component');
  return render(<Component />);
}

describe('Channels Index', () => {
  it('renders the Channels heading', async () => {
    await renderComponent();
    expect(screen.getByText('Channels')).toBeInTheDocument();
  });

  it('renders a row for each channel', async () => {
    await renderComponent();
    expect(screen.getByText('MorWalPiz')).toBeInTheDocument();
    expect(screen.getByText('WalPiz Extra')).toBeInTheDocument();
  });

  it('renders Detail, Edit and Delete actions for each row', async () => {
    await renderComponent();
    expect(screen.getAllByRole('link', { name: 'Detail' })).toHaveLength(2);
    expect(screen.getAllByRole('link', { name: 'Edit' })).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'Delete' })).toHaveLength(2);
  });

  it('opens the delete confirmation modal when Delete is clicked', async () => {
    await renderComponent();
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0]);
    expect(screen.getByText('Confirm Delete')).toBeInTheDocument();
  });

  it('closes the modal when Cancel is clicked', async () => {
    await renderComponent();
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0]);
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await waitFor(() => expect(screen.queryByText('Confirm Delete')).not.toBeInTheDocument());
  });

  it('calls fetcher.submit when modal Delete is confirmed', async () => {
    await renderComponent();
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0]);
    fireEvent.click(screen.getByTestId('delete-modal-confirm'));
    expect(mockSubmit).toHaveBeenCalledWith(
      { id: '1' },
      expect.objectContaining({ method: 'post' })
    );
  });

  it('shows a create link', async () => {
    await renderComponent();
    expect(screen.getByRole('link', { name: /crea/i })).toBeInTheDocument();
  });
});
