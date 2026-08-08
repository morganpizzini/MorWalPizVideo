import { beforeEach, describe, expect, it, vi } from 'vitest';
import type React from 'react';
import { screen, waitFor } from '@testing-library/react';
import { render } from '../test/test-utils';
import { useNavigate } from 'react-router';
import { get } from '@morwalpizvideo/services';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return { ...actual, useNavigate: vi.fn() };
});

vi.mock('@morwalpizvideo/services', () => ({
  get: vi.fn(),
  endpoints: { DASHBOARD_SUMMARY: '/api/dashboard/summary', DASHBOARD_VIDEO_PUBLICATIONS: '/api/dashboard/video-publications' },
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

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(get).mockImplementation((url: string) => Promise.resolve(url.includes('summary') ? {
    totalShortLinks: 3,
    totalShortLinkClicks: 42,
    lastBackOfficeLoginAt: '2026-08-07T10:00:00Z',
    activeUsers: 2,
    publishedVideos: 2,
    activeForms: 1,
    formResponses: 4,
    pendingInsights: 1,
    generatedAt: '2026-08-08T10:00:00Z',
  } : [{ date: '2026-08-07T00:00:00Z', count: 2, videos: [{ id: 'video-1', title: 'Video uno', publishedAt: '2026-08-07T10:00:00Z' }] }]));
});

async function renderComponent() {
  const { default: Component } = await import('./Home');
  render(<Component />);
  await waitFor(() => expect(screen.getByRole('heading', { name: 'Dashboard' })).toBeInTheDocument());
}

describe('Home dashboard', () => {
  it('renders KPI values and the publication section', async () => {
    await renderComponent();
    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Pubblicazione video' })).toBeInTheDocument();
    expect(screen.getByText('Video uno')).toBeInTheDocument();
  });
});