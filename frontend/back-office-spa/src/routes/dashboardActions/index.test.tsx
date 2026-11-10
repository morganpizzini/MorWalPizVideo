import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { transferableAbortController } from 'node:util';
import type { QuickActionsResponse } from '@morwalpizvideo/models';
import {
  getDashboardQuickActions,
  getSelectedChannelId,
  resetDashboardQuickActions,
  saveDashboardQuickActions,
  setSelectedChannelId,
} from '@morwalpizvideo/services';
import { ChannelProvider, useChannelContext } from '../../contexts/ChannelContext';
import { useAppStore } from '../../state/appStore';
import { authService } from '../../services/authService';
import { Action, Component, Loader } from './index';

vi.mock('@morwalpizvideo/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
  getDashboardQuickActions: vi.fn(),
  saveDashboardQuickActions: vi.fn(),
  resetDashboardQuickActions: vi.fn(),
}));

const channels = [
  { channelId: 'one', channelName: 'Canale Uno', yTChannelId: 'one', mine: true },
  { channelId: 'two', channelName: 'Canale Due', yTChannelId: 'two', mine: true },
];

function response(channelId = 'one'): QuickActionsResponse {
  return {
    channelId,
    isConfigured: false,
    canEdit: true,
    updatedAtUtc: null,
    availableActions: [
      { key: 'videos.manage', label: 'Gestisci video', url: '/videos', icon: 'videos' },
    ],
    actions: [{ id: 'video', kind: 'predefined', actionKey: 'videos.manage' }],
    visibleActions: [
      { id: 'video', kind: 'predefined', label: 'Gestisci video', url: '/videos', icon: 'videos' },
    ],
  };
}

function Switch() {
  const { selectChannel } = useChannelContext();
  return <button onClick={() => selectChannel('two')}>Switch channel</button>;
}

async function setup() {
  const router = createMemoryRouter(
    [
      {
        id: 'backoffice',
        path: '/',
        loader: () => ({ session: { userId: 'user-one' } }),
        Component: () => (
          <ChannelProvider channels={channels}>
            <Switch />
            <Outlet />
          </ChannelProvider>
        ),
        children: [{ path: 'dashboard-actions', loader: Loader, action: Action, Component }],
      },
    ],
    { initialEntries: ['/dashboard-actions'] }
  );
  render(<RouterProvider router={router} />);
  await screen.findByRole('heading', { name: 'La tua selezione' });
  return router;
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
  setSelectedChannelId('one');
  useAppStore.setState({ selectedChannelId: 'one', effectivePermissions: ['backoffice.access'] });
  vi.spyOn(authService, 'validateSession').mockResolvedValue({
    userId: 'user-one',
    effectivePermissions: ['backoffice.access'],
  });
  vi.mocked(getDashboardQuickActions).mockImplementation(async () =>
    response(getSelectedChannelId() ?? 'one')
  );
  vi.mocked(saveDashboardQuickActions).mockImplementation(async request => ({
    ...response(),
    isConfigured: true,
    actions: request.actions,
  }));
  vi.mocked(resetDashboardQuickActions).mockResolvedValue(response());
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('personal quick-action editor', () => {
  it('saves literal custom label and URL text, with no route or URL validation', async () => {
    await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Aggiungi link personalizzato' }));
    fireEvent.change(screen.getByLabelText('Etichetta'), { target: { value: '  My <button>  ' } });
    fireEvent.change(screen.getByLabelText('URL'), {
      target: { value: '  missing?Case=%2f#Here  ' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Salva attività' }));
    await screen.findByText('Attività rapide salvate.');
    expect(saveDashboardQuickActions).toHaveBeenCalledWith({
      actions: [
        { id: 'video', kind: 'predefined', actionKey: 'videos.manage' },
        {
          id: expect.any(String),
          kind: 'custom',
          label: '  My <button>  ',
          url: '  missing?Case=%2f#Here  ',
        },
      ],
    });
  });

  it('saves an explicit empty list and resets separately with confirmation', async () => {
    await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Rimuovi attività 1' }));
    fireEvent.click(screen.getByRole('button', { name: 'Salva attività' }));
    await screen.findByText('Attività rapide salvate.');
    expect(saveDashboardQuickActions).toHaveBeenCalledWith({ actions: [] });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fireEvent.click(screen.getByRole('button', { name: 'Ripristina predefinite' }));
    await waitFor(() => expect(resetDashboardQuickActions).toHaveBeenCalledOnce());
  });

  it('preserves the unsaved draft after a failed write and route revalidation', async () => {
    vi.mocked(saveDashboardQuickActions).mockRejectedValue(new Error('Save failed'));
    await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Aggiungi link personalizzato' }));
    fireEvent.change(screen.getByLabelText('Etichetta'), { target: { value: 'Unsaved' } });
    fireEvent.click(screen.getByRole('button', { name: 'Salva attività' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Save failed');
    await waitFor(() => expect(screen.getByLabelText('Etichetta')).toHaveValue('Unsaved'));
    fireEvent.click(screen.getByRole('button', { name: 'Annulla modifiche' }));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('reorders and cancels without changing the saved selection', async () => {
    await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Aggiungi link personalizzato' }));
    fireEvent.click(screen.getByRole('button', { name: 'Sposta su attività 2' }));
    expect(screen.getAllByRole('listitem')[0]).toContainElement(screen.getByLabelText('Etichetta'));
    fireEvent.click(screen.getByRole('button', { name: 'Annulla modifiche' }));
    expect(screen.queryByLabelText('Etichetta')).not.toBeInTheDocument();
    expect(saveDashboardQuickActions).not.toHaveBeenCalled();
  });

  it('discards a dirty draft explicitly on channel change', async () => {
    await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Aggiungi link personalizzato' }));
    fireEvent.click(screen.getByRole('button', { name: 'Switch channel' }));
    await screen.findByText('Contesto cambiato: le modifiche non salvate sono state scartate.');
    await screen.findByText('Canale Due');
    expect(screen.queryByLabelText('Etichetta')).not.toBeInTheDocument();
  });

  it('locks channel selection during a pending mutation and releases it afterwards', async () => {
    let resolve!: (value: QuickActionsResponse) => void;
    vi.mocked(saveDashboardQuickActions).mockReturnValue(
      new Promise(res => {
        resolve = res;
      })
    );
    await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Rimuovi attività 1' }));
    fireEvent.click(screen.getByRole('button', { name: 'Salva attività' }));
    await waitFor(() => expect(saveDashboardQuickActions).toHaveBeenCalledOnce());
    fireEvent.click(screen.getByRole('button', { name: 'Switch channel' }));
    expect(useAppStore.getState().selectedChannelId).toBe('one');
    await act(async () => resolve({ ...response(), actions: [] }));
    await screen.findByText('Attività rapide salvate.');
    fireEvent.click(screen.getByRole('button', { name: 'Switch channel' }));
    await waitFor(() => expect(useAppStore.getState().selectedChannelId).toBe('two'));
  });

  it('renders impersonated preferences read-only', async () => {
    vi.mocked(getDashboardQuickActions).mockResolvedValue({ ...response(), canEdit: false });
    await setup();
    expect(
      screen.getByText("Le preferenze sono in sola lettura durante l'impersonazione.")
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Aggiungi link personalizzato' })).toBeDisabled();
  });
});
