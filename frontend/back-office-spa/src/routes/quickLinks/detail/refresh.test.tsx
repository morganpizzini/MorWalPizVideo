import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { transferableAbortController } from 'node:util';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import {
  createMemoryRouter,
  Outlet,
  RouterProvider,
  useFetcher,
  useLoaderData,
} from 'react-router';
import * as services from '@morwalpizvideo/services';
import { authLoader } from '@/router';
import { authService } from '@/services/authService';
import { ChannelProvider } from '@/contexts/ChannelContext';
import type { Channel } from '@/models/channel';
import { useAppStore } from '@/state/appStore';
import formAction from '../../channels/form/action';
import Component from './Component';

vi.mock('react-router', async () => ({
  ...(await vi.importActual<typeof import('react-router')>('react-router')),
  createBrowserRouter: vi.fn(),
}));

function Shell() {
  const { channels } = useLoaderData<{ channels: Channel[] }>();
  return (
    <ChannelProvider channels={channels}>
      <Outlet />
    </ChannelProvider>
  );
}

function Page() {
  const fetcher = useFetcher();
  return (
    <>
      <Component />
      <button
        onClick={() =>
          fetcher.submit(
            { channelName: 'Owner', publicBaseUrl: 'https://updated.example/' },
            { method: 'post', action: '/channels/owner/edit' }
          )
        }
      >
        Save origin
      </button>
    </>
  );
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
  vi.restoreAllMocks();
  useAppStore.getState().reset();
  services.setSelectedChannelId(null);
});

afterEach(() => vi.unstubAllGlobals());

describe('saved channel destination refresh', () => {
  it('revalidates the real root loader after a save and refreshes context without a reload', async () => {
    let publicBaseUrl = 'https://original.example';
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'owner',
      effectivePermissions: ['backoffice.access'],
    });
    const get = vi
      .spyOn(services, 'get')
      .mockImplementation(async endpoint =>
        endpoint === services.endpoints.CHANNELS_ACCESSIBLE
          ? [{ channelId: 'owner', channelName: 'Owner', yTChannelId: 'UCONE', publicBaseUrl }]
          : { videoBulkImportEnabled: false }
      );
    vi.spyOn(services, 'put').mockImplementation(async (_endpoint, payload) => {
      publicBaseUrl = (payload as { publicBaseUrl: string }).publicBaseUrl;
      return { success: true };
    });
    const router = createMemoryRouter([
      {
        path: '/',
        loader: authLoader,
        Component: Shell,
        children: [
          {
            index: true,
            Component: Page,
            loader: () => ({
              id: 'page',
              channelId: 'owner',
              title: 'Links',
              url: 'community',
              links: [],
            }),
          },
          { path: 'channels/:id/edit', action: formAction },
        ],
      },
    ]);
    render(<RouterProvider router={router} />);
    expect(
      await screen.findByRole('link', { name: 'https://original.example/quick-links/community' })
    ).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Save origin' }));
    expect(
      await screen.findByRole('link', { name: 'https://updated.example/quick-links/community' })
    ).toBeInTheDocument();
    await waitFor(() =>
      expect(useAppStore.getState().accessibleChannels[0]?.publicBaseUrl).toBe(
        'https://updated.example'
      )
    );
    expect(
      get.mock.calls.filter(([endpoint]) => endpoint === services.endpoints.CHANNELS_ACCESSIBLE)
    ).toHaveLength(2);
    expect(services.getSelectedChannelId()).toBe('owner');
    router.dispose();
  });
});
