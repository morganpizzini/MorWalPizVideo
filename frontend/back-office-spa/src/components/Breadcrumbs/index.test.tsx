import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import Breadcrumbs from './index';

vi.mock('react-router', async importOriginal => ({
  ...(await importOriginal<typeof import('react-router')>()),
  useMatches: () => [
    {
      handle: {
        channelBreadcrumb: (channel: { id?: string } | undefined) =>
          channel?.id?.trim() || 'Channel',
      },
      data: {
        data: Promise.resolve({
          id: 'mongo-document-id',
          channelId: 'stable-channel-key',
          channelName: 'Channel',
          yTChannelId: 'UC_EXTERNAL',
        }),
      },
    },
  ],
}));

describe('channel breadcrumbs', () => {
  it('uses the deferred document id while linking with the stable channel route key', async () => {
    const router = createMemoryRouter(
      [
        {
          path: '/channels/:id/edit',
          Component: Breadcrumbs,
        },
      ],
      { initialEntries: ['/channels/stable-channel-key/edit'] }
    );

    render(<RouterProvider router={router} />);

    const documentLink = await screen.findByRole('link', { name: 'mongo-document-id' });
    expect(documentLink).toHaveAttribute('href', '/channels/stable-channel-key');
  });
});
