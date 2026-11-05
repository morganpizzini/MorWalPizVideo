import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, useLoaderData } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Calendar from './Component';

vi.mock('react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react-router')>()),
  useLoaderData: vi.fn(),
}));
vi.mock('@utils/seo', () => ({ default: () => null }));

const event = {
  title: 'Event',
  oldEvent: false,
  startDate: '2026-11-04T10:00:00Z',
  endDate: '2026-11-04T10:00:00Z',
  matchId: 'database-id',
  matchUrl: 'attached-video',
};

const renderCalendar = (data: object) => {
  vi.mocked(useLoaderData).mockReturnValue({ calendar: [data] });
  return renderToStaticMarkup(
    <MemoryRouter>
      <Calendar />
    </MemoryRouter>
  );
};

describe('calendar content destinations', () => {
  beforeEach(() => vi.clearAllMocks());

  it('uses the sole attached video ID even when aggregate and content IDs differ', () => {
    const html = renderCalendar({ ...event, matchVideoId: 'attached-video' });
    expect(html).toContain('href="https://youtu.be/attached-video"');
    expect(html).not.toContain('href="/matches/attached-video"');
    expect(html).toContain('target="_blank"');
  });

  it('uses explicit null for internal navigation without guessing from ID equality', () => {
    const html = renderCalendar({ ...event, matchUrl: 'database-id', matchVideoId: null });
    expect(html).toContain('href="/matches/database-id"');
    expect(html).not.toContain('https://youtu.be/');
  });

  it('keeps the collection URL and hides actions without a destination', () => {
    expect(renderCalendar({ ...event, matchUrl: 'collection-slug', matchVideoId: null })).toContain(
      'href="/matches/collection-slug"'
    );
    expect(renderCalendar({ ...event, matchUrl: '', matchVideoId: null })).not.toContain(
      'event-actions'
    );
  });

  it('accepts responses from an older API during rolling deployment', () => {
    expect(renderCalendar({ ...event, matchUrl: 'database-id' })).toContain(
      'href="https://youtu.be/database-id"'
    );
    expect(renderCalendar({ ...event, matchUrl: 'legacy-slug' })).toContain(
      'href="/matches/legacy-slug"'
    );
  });
});
