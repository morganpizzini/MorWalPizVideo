import { act, render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { transferableAbortController } from 'node:util';
import type { QuickActionsResponse } from '@morwalpizvideo/models';
import { getDashboardQuickActions } from '@morwalpizvideo/services';
import { useAppStore } from '../state/appStore';
import DashboardQuickActions from './DashboardQuickActions';

vi.mock('@morwalpizvideo/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
  getDashboardQuickActions: vi.fn(),
}));

const empty = (channelId = 'one'): QuickActionsResponse => ({
  channelId,
  isConfigured: true,
  canEdit: true,
  updatedAtUtc: null,
  actions: [],
  visibleActions: [],
  availableActions: [],
});

function setup() {
  const router = createMemoryRouter([
    {
      path: '/',
      id: 'backoffice',
      loader: () => ({ session: { userId: 'user-one' } }),
      Component: DashboardQuickActions,
    },
  ]);
  return render(<RouterProvider router={router} />);
}

beforeEach(() => {
  vi.stubGlobal(
    'AbortController',
    class {
      constructor() {
        return transferableAbortController();
      }
    }
  );
  vi.clearAllMocks();
  useAppStore.getState().reset();
  useAppStore.setState({ selectedChannelId: 'one', effectivePermissions: ['backoffice.access'] });
  vi.mocked(getDashboardQuickActions).mockResolvedValue(empty());
});

afterEach(() => vi.unstubAllGlobals());

describe('dynamic dashboard quick actions', () => {
  it('renders custom href and escaped labels literally, without a permission lookup', async () => {
    const url = '/unknown?Case=%2f#Fragment';
    const label = '  <strong>自由</strong>  ';
    vi.mocked(getDashboardQuickActions).mockResolvedValue({
      ...empty(),
      actions: [{ id: 'a', kind: 'custom', url, label }],
      visibleActions: [{ id: 'a', kind: 'custom', url, label, icon: 'link' }],
    });
    const { container } = setup();
    const link = await screen.findByRole('link', { name: '<strong>自由</strong>' });
    expect(link.getAttribute('href')).toBe(url);
    expect(link.textContent).toBe(label);
    expect(container.querySelector('strong')).toBeNull();
  });

  it('retains the editor link for an explicitly empty selection', async () => {
    setup();
    await screen.findByText('Nessuna attività selezionata. Personalizza i tuoi collegamenti.');
    expect(screen.getByRole('link', { name: 'Personalizza' })).toHaveAttribute(
      'href',
      '/dashboard-actions'
    );
  });

  it('shows an explicit failure instead of silently substituting defaults', async () => {
    vi.mocked(getDashboardQuickActions).mockRejectedValue(new Error('offline'));
    setup();
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Impossibile caricare le attività rapide.'
    );
    expect(screen.queryByRole('link', { name: 'Gestisci video' })).not.toBeInTheDocument();
  });

  it('ignores a previous channel result after switching', async () => {
    let resolve!: (value: QuickActionsResponse) => void;
    vi.mocked(getDashboardQuickActions)
      .mockReturnValueOnce(
        new Promise(res => {
          resolve = res;
        })
      )
      .mockResolvedValue(empty('two'));
    setup();
    await waitFor(() => expect(getDashboardQuickActions).toHaveBeenCalledOnce());
    act(() => useAppStore.setState({ selectedChannelId: 'two' }));
    await screen.findByText('Nessuna attività selezionata. Personalizza i tuoi collegamenti.');
    await act(async () =>
      resolve({
        ...empty(),
        actions: [],
        visibleActions: [
          { id: 'old', kind: 'custom', label: 'Old channel', url: '/old', icon: 'link' },
        ],
      })
    );
    expect(screen.queryByText('Old channel')).not.toBeInTheDocument();
  });
});
