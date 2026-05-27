import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render } from '../../../test/test-utils';
import { useFetcher, useNavigate } from 'react-router';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(),
    useNavigate: vi.fn(),
  };
});

const mockSubmit = vi.fn();
const mockNavigate = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(mockNavigate);
  vi.mocked(useFetcher).mockReturnValue({
    state: 'idle',
    data: undefined,
    submit: mockSubmit,
  } as any);
});

async function renderComponent() {
  const { default: Component } = await import('../create/Component');
  return render(<Component />);
}

describe('Create Category', () => {
  it('renders the Create Category heading', async () => {
    await renderComponent();
    expect(screen.getByText('Create Category')).toBeInTheDocument();
  });

  it('renders Title and Description fields', async () => {
    await renderComponent();
    expect(screen.getByLabelText(/title/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/description/i)).toBeInTheDocument();
  });

  it('has the Create button disabled when fields are empty', async () => {
    await renderComponent();
    const createButton = screen.getByRole('button', { name: /create/i });
    expect(createButton).toBeDisabled();
  });

  it('has the Create button disabled when only Title is filled', async () => {
    await renderComponent();
    const titleInput = screen.getByLabelText(/title/i);
    await userEvent.type(titleInput, 'New Category');
    const createButton = screen.getByRole('button', { name: /create/i });
    expect(createButton).toBeDisabled();
  });

  it('enables the Create button when both fields are filled', async () => {
    await renderComponent();
    const titleInput = screen.getByLabelText(/title/i);
    const descInput = screen.getByLabelText(/description/i);
    await userEvent.type(titleInput, 'New Category');
    await userEvent.type(descInput, 'A description');
    const createButton = screen.getByRole('button', { name: /create/i });
    expect(createButton).toBeEnabled();
  });

  it('shows the confirmation modal after form submit', async () => {
    await renderComponent();
    const titleInput = screen.getByLabelText(/title/i);
    const descInput = screen.getByLabelText(/description/i);
    await userEvent.type(titleInput, 'New Category');
    await userEvent.type(descInput, 'A description');
    fireEvent.click(screen.getByRole('button', { name: /create/i }));
    expect(screen.getByText('Confirm Create')).toBeInTheDocument();
    expect(screen.getByText('New Category')).toBeInTheDocument();
    expect(screen.getByText('A description')).toBeInTheDocument();
  });

  it('closes the modal when Cancel is clicked', async () => {
    await renderComponent();
    await userEvent.type(screen.getByLabelText(/title/i), 'Test');
    await userEvent.type(screen.getByLabelText(/description/i), 'Desc');
    fireEvent.click(screen.getByRole('button', { name: /create/i }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await waitFor(() => expect(screen.queryByText('Confirm Create')).not.toBeInTheDocument());
  });

  it('calls fetcher.submit when modal confirm is clicked', async () => {
    await renderComponent();
    await userEvent.type(screen.getByLabelText(/title/i), 'New Category');
    await userEvent.type(screen.getByLabelText(/description/i), 'A description');
    fireEvent.click(screen.getByRole('button', { name: /create/i }));
    fireEvent.click(screen.getByTestId('create-modal-confirm'));
    expect(mockSubmit).toHaveBeenCalledWith(
      { title: 'New Category', description: 'A description' },
      expect.objectContaining({ method: 'post' })
    );
  });

  it('shows field errors when fetcher returns validation errors', async () => {
    vi.mocked(useFetcher).mockReturnValue({
      state: 'idle',
      data: { errors: { title: 'Title is required', generics: [] } },
      submit: mockSubmit,
    } as any);
    await renderComponent();
    expect(screen.getByText('Title is required')).toBeInTheDocument();
  });
});
