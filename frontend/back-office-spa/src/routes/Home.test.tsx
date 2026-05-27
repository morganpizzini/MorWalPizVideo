import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { render } from '../test/test-utils';
import { useNavigate } from 'react-router';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useNavigate: vi.fn(),
  };
});

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
});

async function renderComponent() {
  const { default: Component } = await import('./Home');
  return render(<Component />);
}

describe('Home', () => {
  it('renders the BackOffice heading', async () => {
    await renderComponent();
    expect(screen.getByRole('heading', { name: /backoffice morwalpiz/i })).toBeInTheDocument();
  });

  it('renders the subtitle text', async () => {
    await renderComponent();
    expect(screen.getByText(/seleziona un opzione/i)).toBeInTheDocument();
  });

  it('renders navigation cards for main sections', async () => {
    await renderComponent();
    expect(screen.getByText('Categories Page')).toBeInTheDocument();
    expect(screen.getByText('Querylinks Page')).toBeInTheDocument();
  });

  it('renders the Api Keys card', async () => {
    await renderComponent();
    expect(screen.getByText('Api key')).toBeInTheDocument();
  });
});
