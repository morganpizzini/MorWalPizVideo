import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { useFetcher, useLoaderData } from 'react-router';
import type { ChannelNewsAdmin } from '@morwalpizvideo/models';
import { render } from '../../../test/test-utils';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(),
    useLoaderData: vi.fn(),
  };
});

const entity: ChannelNewsAdmin = {
  id: 'news-1',
  channelId: 'channel-1',
  title: 'Match report',
  subtitle: '',
  descriptionHtml: '<p>Body</p>',
  images: [],
  slug: 'match-report',
  status: 2,
  displayOrder: 0,
  creationDateTime: '2026-01-01T00:00:00Z',
  updatedDateTime: '2026-01-01T00:00:00Z',
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useLoaderData).mockReturnValue([entity]);
  vi.mocked(useFetcher).mockReturnValue({ state: 'idle', data: undefined, submit: vi.fn() } as unknown as ReturnType<typeof useFetcher>);
});

describe('ChannelNews index', () => {
  it('renders the item and edit navigation', async () => {
    const { default: Component } = await import('../index/Component');
    render(<Component />);

    expect(screen.getByText('Match report')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Edit' })).toHaveAttribute('href', '/channelnews/news-1/edit');
    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument();
  });
});
