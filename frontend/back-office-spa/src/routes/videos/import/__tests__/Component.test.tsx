import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { useLoaderData } from 'react-router';
import { render } from '../../../../test/test-utils';
import Component from '../Component';
import { useAppStore } from '../../../../state/appStore';

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
    importVideo: vi.fn(),
  },
}));

vi.mock('../../../../contexts/ChannelContext', () => ({
  useChannelContext: () => ({ selectedChannelId: 'channel-1' }),
}));

beforeEach(() => {
  vi.clearAllMocks();
  useAppStore.getState().reset();
  useAppStore.getState().hydrate({
    user: null,
    effectivePermissions: [],
    featureFlags: { videoBulkImportEnabled: true },
    accessibleChannels: [],
    selectedChannelId: 'channel-1',
    sessionStatus: 'authenticated',
  });
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

  it('shows success for a complete single import', async () => {
    vi.mocked((await import('../../../../services/videoService')).VideoService.importVideo).mockResolvedValue({ videoId: 'new-video', status: 'imported', shortLinkStatus: 'created' });
    render(<Component />);
    fireEvent.click(screen.getByRole('tab', { name: 'Single import' }));
    fireEvent.change(screen.getByLabelText('Video ID *'), { target: { value: 'new-video' } });
    fireEvent.click(screen.getAllByLabelText('Sports')[0]);
    fireEvent.submit(screen.getByRole('button', { name: 'Import video' }).closest('form')!);
    expect(await screen.findByText('Import complete')).toBeInTheDocument();
  });

  it('shows a warning when short-link creation fails after persistence', async () => {
    vi.mocked((await import('../../../../services/videoService')).VideoService.importVideo).mockResolvedValue({ videoId: 'new-video', status: 'imported', shortLinkStatus: 'failed', error: 'Short link failed' });
    render(<Component />);
    fireEvent.click(screen.getByRole('tab', { name: 'Single import' }));
    fireEvent.change(screen.getByLabelText('Video ID *'), { target: { value: 'new-video' } });
    fireEvent.click(screen.getByLabelText('Sports'));
    fireEvent.submit(screen.getByRole('button', { name: 'Import video' }).closest('form')!);
    expect(await screen.findByText('Import completed with warning')).toBeInTheDocument();
  });

  it('shows a warning for an already existing video response', async () => {
    vi.mocked((await import('../../../../services/videoService')).VideoService.importVideo).mockResolvedValue({ videoId: 'old-video', status: 'alreadyExists', error: 'Already imported' });
    render(<Component />);
    fireEvent.click(screen.getByRole('tab', { name: 'Single import' }));
    fireEvent.change(screen.getByLabelText('Video ID *'), { target: { value: 'old-video' } });
    fireEvent.click(screen.getByLabelText('Sports'));
    fireEvent.submit(screen.getByRole('button', { name: 'Import video' }).closest('form')!);
    expect(await screen.findByText('Video already exists')).toBeInTheDocument();
  });

  it('shows a danger toast for a primary import failure', async () => {
    vi.mocked((await import('../../../../services/videoService')).VideoService.importVideo).mockResolvedValue({ status: 500, errors: ['Video could not be persisted'] });
    render(<Component />);
    fireEvent.click(screen.getByRole('tab', { name: 'Single import' }));
    fireEvent.change(screen.getByLabelText('Video ID *'), { target: { value: 'failed-video' } });
    fireEvent.click(screen.getByLabelText('Sports'));
    fireEvent.submit(screen.getByRole('button', { name: 'Import video' }).closest('form')!);
    expect(await screen.findByText('Import failed')).toBeInTheDocument();
  });
});

describe('bulk video import outcomes', () => {
  it('does not show an unconditional success toast for mixed results', async () => {
    vi.mocked((await import('../../../../services/videoService')).VideoService.bulkImport).mockResolvedValue([
      { videoId: 'new-video', status: 'imported', shortLinkStatus: 'created' },
      { videoId: 'old-video', status: 'skipped' },
      { videoId: 'failed-video', status: 'error', error: 'Failed' },
    ]);
    render(<Component />);
    await waitFor(() => expect(screen.getByLabelText(/New video/)).toBeInTheDocument());
    fireEvent.click(screen.getByLabelText(/New video/));
    fireEvent.click(screen.getAllByLabelText('Sports')[0]);
    fireEvent.click(screen.getByRole('button', { name: 'Import selected videos' }));
    expect(await screen.findByText('Bulk import completed with errors')).toBeInTheDocument();
    expect(screen.queryByText('Import complete')).not.toBeInTheDocument();
  });
});

describe('single import when bulk import is disabled', () => {
  it('renders single import without requesting candidates', async () => {
    useAppStore.getState().hydrate({
      user: null,
      effectivePermissions: [],
      featureFlags: { videoBulkImportEnabled: false },
      accessibleChannels: [],
      selectedChannelId: 'channel-1',
      sessionStatus: 'authenticated',
    });

    render(<Component />);

    expect(screen.getByRole('tab', { name: 'Single import' })).toBeInTheDocument();
    expect(screen.queryByRole('tab', { name: 'Bulk import' })).not.toBeInTheDocument();
    expect((await import('../../../../services/videoService')).VideoService.getImportCandidates).not.toHaveBeenCalled();
  });
});