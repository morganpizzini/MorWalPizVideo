import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import { useLoaderData, useFetcher, useNavigate, useSearchParams } from 'react-router';
import { Category } from '@morwalpizvideo/models';

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

const mockCategories: Category[] = [
  { categoryId: '1', title: 'Sport', description: 'Sport events' },
  { categoryId: '2', title: 'Music', description: 'Music videos' },
];

const mockSubmit = vi.fn();

beforeEach(() => {
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useLoaderData).mockReturnValue(mockCategories);
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

describe('Categories Index', () => {
  it('renders the Categories heading', async () => {
    await renderComponent();
    expect(screen.getByText('Categories')).toBeInTheDocument();
  });

  it('renders a row for each category', async () => {
    await renderComponent();
    expect(screen.getByText('Sport')).toBeInTheDocument();
    expect(screen.getByText('Music')).toBeInTheDocument();
    expect(screen.getByText('Sport events')).toBeInTheDocument();
    expect(screen.getByText('Music videos')).toBeInTheDocument();
  });

  it('renders Detail, Edit and Delete actions for each row', async () => {
    await renderComponent();
    const detailLinks = screen.getAllByRole('link', { name: 'Detail' });
    const editLinks = screen.getAllByRole('link', { name: 'Edit' });
    const deleteButtons = screen.getAllByRole('button', { name: 'Delete' });
    expect(detailLinks).toHaveLength(2);
    expect(editLinks).toHaveLength(2);
    expect(deleteButtons).toHaveLength(2);
  });

  it('opens the delete confirmation modal when Delete is clicked', async () => {
    await renderComponent();
    const deleteButtons = screen.getAllByRole('button', { name: 'Delete' });
    fireEvent.click(deleteButtons[0]);
    expect(screen.getByText('Confirm Delete')).toBeInTheDocument();
    expect(screen.getAllByText('Sport').length).toBeGreaterThan(0);
  });

  it('closes the modal when Cancel is clicked', async () => {
    await renderComponent();
    const deleteButtons = screen.getAllByRole('button', { name: 'Delete' });
    fireEvent.click(deleteButtons[0]);
    const cancelButton = screen.getByRole('button', { name: 'Cancel' });
    fireEvent.click(cancelButton);
    await waitFor(() => expect(screen.queryByText('Confirm Delete')).not.toBeInTheDocument());
  });

  it('calls fetcher.submit when the modal Delete button is confirmed', async () => {
    await renderComponent();
    const deleteButtons = screen.getAllByRole('button', { name: 'Delete' });
    fireEvent.click(deleteButtons[0]);
    const confirmButton = screen.getByTestId('delete-modal-confirm');
    fireEvent.click(confirmButton);
    expect(mockSubmit).toHaveBeenCalledWith(
      { id: '1' },
      expect.objectContaining({ method: 'post' })
    );
  });

  it('shows a create link', async () => {
    await renderComponent();
    const createLink = screen.getByRole('link', { name: /crea/i });
    expect(createLink).toBeInTheDocument();
    expect(createLink).toHaveAttribute('href', '/create');
  });

  it('displays error messages when fetcher returns errors', async () => {
    vi.mocked(useFetcher).mockReturnValue({
      state: 'idle',
      data: { errors: { generics: ['Something went wrong'] } },
      submit: mockSubmit,
    } as any);
    await renderComponent();
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });
});
