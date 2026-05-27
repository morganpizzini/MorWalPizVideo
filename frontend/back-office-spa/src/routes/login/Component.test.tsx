import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render } from '../../test/test-utils';
import { useActionData, useNavigation } from 'react-router';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useActionData: vi.fn(),
    useNavigation: vi.fn(),
    Form: ({ children, ...props }: React.FormHTMLAttributes<HTMLFormElement> & { children?: React.ReactNode }) => <form {...props}>{children}</form>,
  };
});

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useActionData).mockReturnValue(undefined);
  vi.mocked(useNavigation).mockReturnValue({ state: 'idle' } as any);
});

async function renderComponent() {
  const { default: Component } = await import('./Component');
  return render(<Component />);
}

describe('Login', () => {
  it('renders the Sign in heading', async () => {
    await renderComponent();
    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument();
  });

  it('renders username and password fields', async () => {
    await renderComponent();
    expect(screen.getByLabelText(/username or email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
  });

  it('renders the Sign in submit button', async () => {
    await renderComponent();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('shows an error message when actionData contains a message', async () => {
    vi.mocked(useActionData).mockReturnValue({ message: 'Invalid credentials' });
    await renderComponent();
    expect(screen.getByText('Login Failed')).toBeInTheDocument();
    expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
  });

  it('disables the submit button while submitting', async () => {
    vi.mocked(useNavigation).mockReturnValue({ state: 'submitting' } as any);
    await renderComponent();
    expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled();
  });

  it('accepts input in username and password fields', async () => {
    await renderComponent();
    const usernameInput = screen.getByLabelText(/username or email/i);
    const passwordInput = screen.getByLabelText(/password/i);
    await userEvent.type(usernameInput, 'admin');
    await userEvent.type(passwordInput, 'secret');
    expect(usernameInput).toHaveValue('admin');
    expect(passwordInput).toHaveValue('secret');
  });

  it('shows the blocked message when retryAfter is set', async () => {
    vi.mocked(useActionData).mockReturnValue({ message: 'Rate limited', retryAfter: 30 });
    await renderComponent();
    expect(screen.getByText('Account Temporarily Locked')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /blocked/i })).toBeDisabled();
  });
});
