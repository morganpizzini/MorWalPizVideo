import { describe, expect, it, beforeEach, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { get } from '@morwalpizvideo/services';
import { render } from '../../../test/test-utils';

vi.mock('@morwalpizvideo/services', () => ({ get: vi.fn() }));

const mockGet = vi.mocked(get);

const diagnostics = {
  status: 'Healthy',
  checks: {
    database: {
      status: 'Healthy',
      description: 'Available',
      durationMilliseconds: 12.4,
    },
  },
  recentProblems: [
    {
      timestampUtc: '2026-08-04T10:00:00Z',
      category: 'Backend',
      message: 'A problem occurred',
      properties: {},
    },
  ],
};

async function renderComponent() {
  const { default: Diagnostics } = await import('../index');
  return render(<Diagnostics />);
}

describe('Diagnostics', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading then health and recent problems', async () => {
    mockGet.mockResolvedValue(diagnostics);
    await renderComponent();

    expect(screen.getByText('Caricamento diagnostics...')).toBeInTheDocument();
    expect(await screen.findByText('A problem occurred')).toBeInTheDocument();
    expect(screen.getAllByText('Healthy')).not.toHaveLength(0);
  });

  it('shows the empty recent-problems state', async () => {
    mockGet.mockResolvedValue({ ...diagnostics, recentProblems: [] });
    await renderComponent();

    expect(await screen.findByText('Nessun problema live registrato.')).toBeInTheDocument();
  });

  it('shows a rejected request failure state', async () => {
    mockGet.mockRejectedValue(new Error('network failure'));
    await renderComponent();

    expect(await screen.findByText('Impossibile caricare i diagnostics del backend.')).toBeInTheDocument();
  });

  it('shows errors returned in the shared get envelope', async () => {
    mockGet.mockResolvedValue({ errors: ['Backend unavailable'] });
    await renderComponent();

    expect(await screen.findByText('Backend unavailable')).toBeInTheDocument();
  });

  it('shows access denial for a forbidden response envelope', async () => {
    mockGet.mockResolvedValue({ errors: ['403 Forbidden'] });
    await renderComponent();

    expect(await screen.findByText('Accesso negato ai diagnostics del backend.')).toBeInTheDocument();
  });
});