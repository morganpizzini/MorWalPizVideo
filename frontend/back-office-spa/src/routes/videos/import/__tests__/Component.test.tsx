import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
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

vi.mock('../../../../contexts/ChannelContext', () => ({
  useChannelContext: () => ({ selectedChannelId: 'channel-1' }),
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

    expect(screen.getByLabelText('Published from (UTC) *')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText(/New video/)).toBeInTheDocument());
    fireEvent.click(screen.getByLabelText(/New video/));
    expect(screen.getByText('Append to Existing collection')).toBeInTheDocument();
    expect(screen.queryByLabelText(/Old video/)).not.toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Single import' })).toBeInTheDocument();
  });
});