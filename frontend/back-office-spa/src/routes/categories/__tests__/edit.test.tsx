import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
const mockNavigate = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(mockNavigate);
  vi.mocked(useLoaderData).mockReturnValue(mockCategory);
  vi.mocked(useFetcher).mockReturnValue({
    state: 'idle',
    data: undefined,
    submit: mockSubmit,
  } as any);
});

async function renderComponent() {
  const { default: Component } = await import('../edit/Component');
  return render(<Component />);
}

describe('Edit Category', () => {
  it('renders the Edit Category heading with entity title', async () => {
    await renderComponent();
    expect(screen.getByText('Edit Category: Sport')).toBeInTheDocument();
  });

  it('pre-populates the Title and Description fields', async () => {
    await renderComponent();
    expect(screen.getByDisplayValue('Sport')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Sport events')).toBeInTheDocument();
  });

  it('has the Update button disabled when no changes have been made', async () => {
    await renderComponent();
    const updateButton = screen.getByRole('button', { name: /update/i });
    expect(updateButton).toBeDisabled();
  });

  it('enables the Update button when a field is changed', async () => {
    await renderComponent();
    const titleInput = screen.getByDisplayValue('Sport');
    await userEvent.clear(titleInput);
    await userEvent.type(titleInput, 'Updated Sport');
    const updateButton = screen.getByRole('button', { name: /update/i });
    expect(updateButton).toBeEnabled();
  });

  it('opens the confirmation modal on form submit', async () => {
    await renderComponent();
    const titleInput = screen.getByDisplayValue('Sport');
    await userEvent.clear(titleInput);
    await userEvent.type(titleInput, 'Updated Sport');
    fireEvent.click(screen.getByRole('button', { name: /update/i }));
    expect(screen.getByText('Confirm Edit')).toBeInTheDocument();
  });

  it('closes the modal when Cancel is clicked', async () => {
    await renderComponent();
    const titleInput = screen.getByDisplayValue('Sport');
    await userEvent.clear(titleInput);
    await userEvent.type(titleInput, 'Updated Sport');
    fireEvent.click(screen.getByRole('button', { name: /update/i }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await waitFor(() => expect(screen.queryByText('Confirm Edit')).not.toBeInTheDocument());
  });

  it('calls fetcher.submit when the modal confirm is clicked', async () => {
    await renderComponent();
    const titleInput = screen.getByDisplayValue('Sport');
    await userEvent.clear(titleInput);
    await userEvent.type(titleInput, 'Updated Sport');
    fireEvent.click(screen.getByRole('button', { name: /update/i }));
    fireEvent.click(screen.getByTestId('edit-modal-confirm'));
    expect(mockSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Updated Sport', description: 'Sport events' }),
      expect.objectContaining({ method: 'post' })
    );
  });
});
