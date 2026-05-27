import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import { useLoaderData, useFetcher, useNavigate, useSearchParams } from 'react-router';
import { QueryLink } from '@morwalpizvideo/models';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useLoaderData: vi.fn(),
    useFetcher: vi.fn(),
    useNavigate: vi.fn(),
    useSearchParams: vi.fn(),
  };
});

const mockQueryLinks: QueryLink[] = [
  { queryLinkId: '1', title: 'Instagram Link', value: 'utm_source=instagram' },
  { queryLinkId: '2', title: 'Facebook Link', value: 'utm_source=facebook' },
];

const mockSubmit = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useLoaderData).mockReturnValue(mockQueryLinks);
  vi.mocked(useFetcher).mockReturnValue({
    state: 'idle',
    data: undefined,
    submit: mockSubmit,
  } as any);
  vi.mocked(useSearchParams).mockReturnValue([new URLSearchParams(), vi.fn()] as any);
});

async function renderComponent() {
  const { default: Component } = await import('../index/Component');
  return render(<Component />);
}

describe('QueryLinks Index', () => {
  it('renders the Query Links heading', async () => {
    await renderComponent();
    expect(screen.getByText('Query Links')).toBeInTheDocument();
  });

  it('renders a row for each query link', async () => {
    await renderComponent();
    expect(screen.getByText('Instagram Link')).toBeInTheDocument();
    expect(screen.getByText('Facebook Link')).toBeInTheDocument();
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
});
