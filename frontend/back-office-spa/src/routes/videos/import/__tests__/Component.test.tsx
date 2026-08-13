import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { useLoaderData } from 'react-router';
import { render } from '../../../../test/test-utils';
import Component from '../Component';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return { ...actual, useLoaderData: vi.fn() };
});

vi.mock('../../../../services/videoService', () => ({
  VideoService: {
    getImportCandidates: vi.fn().mockResolvedValue([
      { videoId: 'new-video', title: 'New video', publishedAt: '2026-01-02T00:00:00Z', alreadyImported: false },
      { videoId: 'old-video', title: 'Old video', publishedAt: '2026-01-03T00:00:00Z', alreadyImported: true },
    ]),
    bulkImport: vi.fn(),
  },
}));

beforeEach(() => {
  vi.mocked(useLoaderData).mockReturnValue({
    categories: [{ categoryId: 'cat-1', title: 'Sports' }],
    channels: [{ channelId: 'channel-1', channelName: 'Main', yTChannelId: 'yt-1', mine: true }],
    targets: [{ contentId: 'content-1', title: 'Existing collection', videoCount: 2 }],
  });
});

describe('bulk video import', () => {
  it('renders the authorized channel, date, candidates, and append target', async () => {
    render(<Component />);

    expect(screen.getByLabelText('Authorized channel *')).toHaveValue('channel-1');
    expect(screen.getByLabelText('Published from (UTC) *')).toBeInTheDocument();
    expect(screen.getByText('Existing collection (2 videos)')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText(/New video/)).toBeInTheDocument());
    expect(screen.getByLabelText(/Old video/)).toBeDisabled();
  });
});