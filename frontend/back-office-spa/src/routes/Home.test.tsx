import { beforeEach, describe, expect, it, vi } from 'vitest';
import type React from 'react';
import { act, fireEvent, screen, waitFor } from '@testing-library/react';
import { render } from '../test/test-utils';
import { useAppStore } from '../state/appStore';
import { useNavigate } from 'react-router';
import { get, getDashboardQuickActions } from '@morwalpizvideo/services';
import { useRouteLoaderData } from 'react-router';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return { ...actual, useNavigate: vi.fn(), useRouteLoaderData: vi.fn() };
});

vi.mock('@morwalpizvideo/services', () => ({
  get: vi.fn(),
  getDashboardQuickActions: vi.fn(),
  endpoints: {
    DASHBOARD_SUMMARY: '/api/dashboard/summary',
    DASHBOARD_VIDEO_PUBLICATIONS: '/api/dashboard/video-publications',
  },
}));

vi.mock('recharts', () => ({
  Bar: () => null,
  BarChart: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  CartesianGrid: () => null,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Tooltip: () => null,
  XAxis: () => null,
  YAxis: () => null,
}));

interface DashboardSummary {
  totalShortLinks: number;
  totalShortLinkClicks: number;
  lastBackOfficeLoginAt: string | null;
  activeUsers: number;
  publishedVideos: number;
  activeForms: number;
  formResponses: number;
  pendingInsights: number;
  generatedAt: string;
}

interface PublicationDay {
  date: string;
  count: number;
  videos: { id: string; title: string; publishedAt: string }[];
}

function summary(publishedVideos = 2): DashboardSummary {
  return {
    totalShortLinks: 3,
    totalShortLinkClicks: 42,
    lastBackOfficeLoginAt: '2026-08-07T10:00:00Z',
    activeUsers: 2,
    publishedVideos,
    activeForms: 1,
    formResponses: 4,
    pendingInsights: 1,
    generatedAt: '2026-08-08T10:00:00Z',
  };
}

function publications(title = 'Video uno'): PublicationDay[] {
  return [
    {
      date: '2026-08-07T00:00:00Z',
      count: 1,
      videos: [{ id: 'video-1', title, publishedAt: '2026-08-07T10:00:00Z' }],
    },
  ];
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(res => {
    resolve = res;
  });
  return { promise, resolve };
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useRouteLoaderData).mockReturnValue({ session: { userId: 'user-one' } });
  useAppStore.getState().reset();
  useAppStore.setState({
    selectedChannelId: 'channel-one',
    effectivePermissions: ['backoffice.access', 'videos.view', 'pages.view', 'pages.create'],
  });
  vi.mocked(get).mockImplementation((url: string) =>
    Promise.resolve(url.includes('summary') ? summary() : publications())
  );
  vi.mocked(getDashboardQuickActions).mockImplementation(async () => {
    const permissions = useAppStore.getState().effectivePermissions;
    const definitions = [
      {
        key: 'videos.manage',
        label: 'Gestisci video',
        url: '/videos',
        icon: 'videos',
        permission: 'videos.view',
      },
      {
        key: 'articles.create',
        label: 'Scrivi articolo',
        url: '/blogposts/create',
        icon: 'article-create',
        permission: 'pages.create',
      },
      {
        key: 'articles.list',
        label: 'Tutti gli articoli',
        url: '/blogposts',
        icon: 'articles',
        permission: 'pages.view',
      },
    ].filter(item => permissions.includes(item.permission));
    return {
      channelId: useAppStore.getState().selectedChannelId!,
      isConfigured: false,
      canEdit: true,
      updatedAtUtc: null,
      availableActions: definitions,
      actions: definitions.map(item => ({
        id: item.key,
        kind: 'predefined' as const,
        actionKey: item.key,
      })),
      visibleActions: definitions.map(item => ({
        ...item,
        id: item.key,
        kind: 'predefined' as const,
      })),
    };
  });
});

async function renderComponent() {
  const { default: Component } = await import('./Home');
  render(<Component />);
  await screen.findByText('42');
}

describe('Home dashboard', () => {
  it('keeps quick actions above the dashboard analytics', async () => {
    await renderComponent();

    expect(screen.getByRole('link', { name: 'Gestisci video' })).toHaveAttribute('href', '/videos');
    expect(screen.getByRole('link', { name: 'Scrivi articolo' })).toHaveAttribute(
      'href',
      '/blogposts/create'
    );
    expect(screen.getByRole('link', { name: 'Tutti gli articoli' })).toHaveAttribute(
      'href',
      '/blogposts'
    );
    expect(screen.getByRole('heading', { name: 'Pubblicazione video' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Video uno' })).toHaveAttribute(
      'href',
      '/videos/video-1'
    );
  });

  it('shows article creation without article listing when only create permission is granted', async () => {
    useAppStore.setState({
      effectivePermissions: ['backoffice.access', 'pages.create'],
    });

    const { default: Component } = await import('./Home');
    render(<Component />);

    expect(await screen.findByRole('link', { name: 'Scrivi articolo' })).toHaveAttribute(
      'href',
      '/blogposts/create'
    );
    expect(screen.queryByRole('link', { name: 'Tutti gli articoli' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Gestisci video' })).not.toBeInTheDocument();
  });

  it('keeps allowed actions available when analytics fail and retries both dashboard reads', async () => {
    vi.mocked(get).mockRejectedValueOnce(new Error('dashboard unavailable'));
    const { default: Component } = await import('./Home');
    render(<Component />);

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Impossibile caricare i dati della dashboard.'
    );
    expect(screen.getByRole('link', { name: 'Gestisci video' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Riprova/i }));

    await screen.findByText('42');
    expect(get).toHaveBeenCalledTimes(4);
  });

  it('does not request channel-scoped analytics without a selected channel', async () => {
    useAppStore.setState({ selectedChannelId: null });
    const { default: Component } = await import('./Home');
    render(<Component />);

    expect(
      await screen.findByText('Seleziona un canale accessibile per visualizzare i dati operativi.')
    ).toBeInTheDocument();
    expect(get).not.toHaveBeenCalled();
    expect(screen.queryByRole('link', { name: 'Scrivi articolo' })).not.toBeInTheDocument();
    expect(getDashboardQuickActions).not.toHaveBeenCalled();
  });

  it('shows an explicit empty state when the selected channel has no recent publications', async () => {
    vi.mocked(get).mockImplementation((url: string) =>
      Promise.resolve(url.includes('summary') ? summary(0) : [])
    );
    const { default: Component } = await import('./Home');
    render(<Component />);

    expect(
      await screen.findByText('Nessun video pubblicato negli ultimi 21 giorni.')
    ).toBeInTheDocument();
  });

  it('ignores a previous channel response after the selected channel changes', async () => {
    const firstSummary = deferred<DashboardSummary>();
    const secondSummary = deferred<DashboardSummary>();
    const firstPublications = deferred<PublicationDay[]>();
    const secondPublications = deferred<PublicationDay[]>();
    let summaryRequests = 0;
    let publicationRequests = 0;

    vi.mocked(get).mockImplementation((url: string) => {
      if (url.includes('summary')) {
        summaryRequests += 1;
        return summaryRequests === 1 ? firstSummary.promise : secondSummary.promise;
      }

      publicationRequests += 1;
      return publicationRequests === 1 ? firstPublications.promise : secondPublications.promise;
    });

    const { default: Component } = await import('./Home');
    render(<Component />);
    await waitFor(() => expect(summaryRequests).toBe(1));

    act(() => useAppStore.setState({ selectedChannelId: 'channel-two' }));
    await waitFor(() => expect(summaryRequests).toBe(2));
    await waitFor(() => expect(publicationRequests).toBe(2));

    await act(async () => {
      secondSummary.resolve(summary(99));
      secondPublications.resolve(publications('Channel two video'));
    });
    expect(await screen.findByText('Channel two video')).toBeInTheDocument();
    expect(screen.getByText('99')).toBeInTheDocument();

    await act(async () => {
      firstSummary.resolve(summary(11));
      firstPublications.resolve(publications('Channel one video'));
    });
    expect(screen.getByText('Channel two video')).toBeInTheDocument();
    expect(screen.queryByText('Channel one video')).not.toBeInTheDocument();
  });
});
