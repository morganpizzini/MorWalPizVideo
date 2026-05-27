import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import { useLoaderData, useFetcher, useNavigate } from 'react-router';
import { Category } from '@morwalpizvideo/models';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useLoaderData: vi.fn(),
    useFetcher: vi.fn(),
    useNavigate: vi.fn(),
  };
});

const mockCategory: Category = {
  categoryId: '1',
  title: 'Sport',
  description: 'Sport events',
};

const mockSubmit = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useLoaderData).mockReturnValue(mockCategory);
  vi.mocked(useFetcher).mockReturnValue({
    state: 'idle',
    data: undefined,
    submit: mockSubmit,
  } as any);
});

async function renderComponent() {
  const { default: Component } = await import('../detail/Component');
  return render(<Component />);
}

describe('Category Detail', () => {
  it('renders the category title in the header', async () => {
    await renderComponent();
    expect(screen.getAllByText('Sport').length).toBeGreaterThan(0);
  });

  it('renders the Category Details panel', async () => {
    await renderComponent();
    expect(screen.getByText('Category Details')).toBeInTheDocument();
  });

  it('shows the category title and description', async () => {
    await renderComponent();
    expect(screen.getByText('Sport events')).toBeInTheDocument();
  });

  it('renders an Edit link', async () => {
    await renderComponent();
    const editLink = screen.getByRole('link', { name: /modifica/i });
    expect(editLink).toBeInTheDocument();
    expect(editLink).toHaveAttribute('href', '/categories/1/edit');
  });

  it('renders a Delete button', async () => {
    await renderComponent();
    expect(screen.getByRole('button', { name: /elimina/i })).toBeInTheDocument();
  });

  it('opens the delete confirmation modal when Delete is clicked', async () => {
    await renderComponent();
    fireEvent.click(screen.getByRole('button', { name: /elimina/i }));
    expect(screen.getByText('Confirm Delete')).toBeInTheDocument();
    expect(screen.getAllByText('Sport').length).toBeGreaterThan(0);
  });

  it('closes the modal when Cancel is clicked', async () => {
    await renderComponent();
    fireEvent.click(screen.getByRole('button', { name: /elimina/i }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await waitFor(() => expect(screen.queryByText('Confirm Delete')).not.toBeInTheDocument());
  });

  it('calls fetcher.submit when confirm delete is clicked', async () => {
    await renderComponent();
    fireEvent.click(screen.getByRole('button', { name: /elimina/i }));
    fireEvent.click(screen.getByTestId('delete-modal-confirm'));
    expect(mockSubmit).toHaveBeenCalledWith(
      { id: '1' },
      expect.objectContaining({ method: 'post' })
    );
  });
});
