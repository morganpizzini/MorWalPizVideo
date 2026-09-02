import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useFetcher, useLoaderData, useNavigate } from 'react-router';
import { LinkType } from '@morwalpizvideo/models';
import type { CategoryRef, Match, VideoRef } from '@morwalpizvideo/models';
import { render } from '../../../../test/test-utils';
import Component from '../Component';

const mockToastShow = vi.hoisted(() => vi.fn());

vi.mock('@components/ToastNotification/ToastContext', async importOriginal => {
  const actual = await importOriginal<typeof import('@components/ToastNotification/ToastContext')>();
  return {
    ...actual,
    useToast: () => ({ show: mockToastShow }),
  };
});

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(),
    useLoaderData: vi.fn(),
    useNavigate: vi.fn(),
  };
});

const mockSaveSubmit = vi.fn();
const mockAddSubmit = vi.fn();
const mockNavigate = vi.fn();
const categories: CategoryRef[] = [{ id: 'category-1', title: 'News' }];

const match = {
  id: 'match-1',
  contentId: 'content-1',
  title: 'A video',
  description: 'Description',
  url: 'https://example.com/video',
  thumbnailVideoId: 'existing-video',
  videoRefs: [],
  categories,
  contentType: 0,
  isLink: false,
  creationDateTime: '2026-01-01T00:00:00.000Z',
  shortLinks: [],
} as Match;

const getManageCategoryCheckbox = (): HTMLElement =>
  document.querySelector('[id^="new-videoref"][type="checkbox"]') as HTMLElement;

type FetcherData = {
  success?: boolean;
  videoRef?: VideoRef;
  errors?: { generics?: string[] };
};

let saveFetcher: { state: 'idle' | 'submitting'; data?: FetcherData; submit: typeof mockSaveSubmit };
let addFetcher: { state: 'idle' | 'submitting'; data?: FetcherData; submit: typeof mockAddSubmit };

beforeEach(() => {
  vi.clearAllMocks();
  mockSaveSubmit.mockReset();
  mockAddSubmit.mockReset();
  saveFetcher = { state: 'idle', data: undefined, submit: mockSaveSubmit };
  addFetcher = { state: 'idle', data: undefined, submit: mockAddSubmit };
  vi.mocked(useNavigate).mockReturnValue(mockNavigate);
  vi.mocked(useLoaderData).mockReturnValue({ match, categories });
  let fetcherCall = 0;
  vi.mocked(useFetcher).mockImplementation(() =>
    (fetcherCall++ % 2 === 0 ? saveFetcher : addFetcher) as unknown as ReturnType<typeof useFetcher>
  );
});

async function renderComponent() {
  return render(<Component />);
}

describe('Edit Video', () => {
  it('submits a minimal add request and renders the row only after success', async () => {
    const user = userEvent.setup();
    const view = await renderComponent();

    await user.type(screen.getByLabelText('YouTube ID'), 'new-video');
    await user.click(getManageCategoryCheckbox());
    await user.click(screen.getByRole('button', { name: 'Add Video Reference' }));

    expect(screen.queryByText('new-video')).not.toBeInTheDocument();
    expect(mockAddSubmit).toHaveBeenCalledWith(
      {
        _intent: 'addVideoReference',
        youtubeId: 'new-video',
        categories: JSON.stringify(['category-1']),
      },
      { method: 'post', action: location.pathname }
    );
    expect(mockSaveSubmit).not.toHaveBeenCalled();

    addFetcher.state = 'idle';
    addFetcher.data = {
      success: true,
      videoRef: {
        youtubeId: 'new-video',
        categories,
        channelIds: ['channel-1'],
        title: 'Server title',
        description: '',
        publishedAt: '',
        creationDateTime: '2026-01-01T00:00:00.000Z',
        shortLinkCode: 'abc12',
        shortLinkStatus: 'created',
      },
    };
    view.rerender(<Component />);

    await waitFor(() => expect(screen.getByText('new-video')).toBeInTheDocument());
    expect(screen.getByText('/abc12')).toBeInTheDocument();
    expect(mockToastShow).toHaveBeenCalledWith(
      'Success',
      'Video reference added successfully',
      { variant: 'success' }
    );
  });

  it('does not render a row and shows an error toast when adding fails', async () => {
    const user = userEvent.setup();
    const view = await renderComponent();

    await user.type(screen.getByLabelText('YouTube ID'), 'new-video');
    await user.click(getManageCategoryCheckbox());
    await user.click(screen.getByRole('button', { name: 'Add Video Reference' }));

    addFetcher.state = 'idle';
    addFetcher.data = { success: false, errors: { generics: ['Duplicate reference'] } };
    view.rerender(<Component />);

    await waitFor(() => {
      expect(screen.queryByText('new-video')).not.toBeInTheDocument();
      expect(mockToastShow).toHaveBeenCalledWith(
        'Video reference add failed',
        'Duplicate reference',
        { variant: 'danger' }
      );
    });
  });

  it('disables Save Changes while the route action is submitting', async () => {
    saveFetcher.state = 'submitting';

    await renderComponent();

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled();
  });

  it('shows an error toast when the action returns an API error', async () => {
    saveFetcher.data = {
      success: false,
      errors: { generics: ['The video could not be updated.'] },
    };

    await renderComponent();

    await waitFor(() => {
      expect(mockToastShow).toHaveBeenCalledWith(
        'Video update failed',
        'The video could not be updated.',
        { variant: 'danger' }
      );
    });
  });

  it('shows a success toast and navigates after a successful save', async () => {
    saveFetcher.data = { success: true };

    await renderComponent();

    await waitFor(() => {
      expect(mockToastShow).toHaveBeenCalledWith(
        'Success',
        'Video updated successfully',
        { variant: 'success' }
      );
      expect(mockNavigate).toHaveBeenCalledWith('..');
    });
  });

  it('submits metadata without video reference controls', async () => {
    const user = userEvent.setup();
    await renderComponent();

    await user.clear(screen.getByLabelText('Title'));
    await user.type(screen.getByLabelText('Title'), 'Updated title');
    fireEvent.submit(screen.getByRole('button', { name: 'Save Changes' }).closest('form')!);

    const submittedFormData = mockSaveSubmit.mock.calls[0][0] as FormData;
    expect(submittedFormData.get('title')).toBe('Updated title');
    expect(submittedFormData.get('videoRefs')).toBeNull();
    expect(mockAddSubmit).not.toHaveBeenCalled();
  });

  it('disables Add Video Reference while its request is submitting', async () => {
    addFetcher.state = 'submitting';
    await renderComponent();

    expect(screen.getByRole('button', { name: 'Adding...' })).toBeDisabled();
  });

  it('projects the canonical short link after the edit route is reloaded', async () => {
    const persistedVideoRef: VideoRef = {
      youtubeId: 'persisted-video',
      categories,
      channelIds: ['channel-1'],
      title: 'Persisted title',
      description: '',
      publishedAt: '',
      creationDateTime: '2026-01-01T00:00:00.000Z',
    };
    vi.mocked(useLoaderData).mockReturnValue({
      match: {
        ...match,
        videoRefs: [persistedVideoRef],
        shortLinks: [{
          shortLinkId: 'short-link-1',
          code: 'persisted-code',
          target: 'persisted-video',
          linkType: LinkType.YouTubeVideo,
          queryLinkIds: [],
          message: '',
          clicksCount: 0,
          videoId: 'persisted-video',
        }],
      },
      categories,
    });

    await renderComponent();

    expect(screen.getByText('/persisted-code')).toBeInTheDocument();
  });
});
