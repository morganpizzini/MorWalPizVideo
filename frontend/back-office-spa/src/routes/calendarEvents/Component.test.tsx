import React from 'react';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, useFetcher, useNavigate, useRevalidator } from 'react-router';
import { getCalendarEventById, fetchCalendarCategories } from '@morwalpizvideo/services';
import { useResolvedLoaderData } from '@/router/asyncData';
import { useToast } from '@components/ToastNotification/ToastContext';
import Edit from './edit/Component';
import Create from './create/Component';
import Detail from './detail/Component';
import Index from './index/Component';

vi.mock('react-router', async () => ({
  ...(await vi.importActual<typeof import('react-router')>('react-router')),
  useFetcher: vi.fn(),
  useNavigate: vi.fn(),
  useRevalidator: vi.fn(),
}));
vi.mock('@/router/asyncData', () => ({ useResolvedLoaderData: vi.fn() }));
vi.mock('@components/ToastNotification/ToastContext', () => ({ useToast: vi.fn() }));
vi.mock('@morwalpizvideo/services', async () => ({
  ...(await vi.importActual<typeof import('@morwalpizvideo/services')>('@morwalpizvideo/services')),
  getCalendarEventById: vi.fn(),
  fetchCalendarCategories: vi.fn(async () => []),
}));

const event = {
  id: 'event-id',
  title: 'Original',
  description: 'Description',
  startDate: '2026-01-01T23:30:00',
  endDate: '2026-01-02T23:30:00',
  matchId: 'match-id',
  revision: 1,
  categories: [{ id: 'legacy-category', title: 'Legacy category' }],
  channelId: 'channel-a',
};
const fetcher = {
  state: 'idle',
  data: undefined as unknown,
  submit: vi.fn(),
  Form: ({ children, ...props }: React.ComponentProps<'form'>) => (
    <form {...props}>{children}</form>
  ),
};

beforeEach(() => {
  vi.clearAllMocks();
  fetcher.state = 'idle';
  fetcher.data = undefined;
  vi.mocked(useToast).mockReturnValue({ show: vi.fn() } as never);
  vi.mocked(useFetcher).mockReturnValue(fetcher as never);
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useRevalidator).mockReturnValue({ state: 'idle', revalidate: vi.fn() } as never);
  vi.mocked(useResolvedLoaderData).mockReturnValue({ calendarEvent: event, categories: [] });
});

describe('Calendar editor hardening', () => {
  it('submits persisted identity, revision and legacy category references without shifting dates', () => {
    render(
      <MemoryRouter>
        <Edit />
      </MemoryRouter>
    );
    fireEvent.change(screen.getByLabelText('Title*'), { target: { value: 'Renamed' } });
    fireEvent.click(screen.getByRole('button', { name: 'Update' }));
    const payload = fetcher.submit.mock.calls[0][0] as FormData;
    expect(payload.get('id')).toBe(event.id);
    expect(payload.get('revision')).toBe('1');
    expect(payload.get('newTitle')).toBe('Renamed');
    expect(payload.get('startDate')).toBe('2026-01-01');
    expect(JSON.parse(String(payload.get('categories')))).toEqual(event.categories);
  });

  it('retains the draft on conflict and only resets values/revision after explicit ID reload', async () => {
    const view = render(
      <MemoryRouter>
        <Edit />
      </MemoryRouter>
    );
    fireEvent.change(screen.getByLabelText('Title*'), { target: { value: 'Unsaved draft' } });
    fetcher.data = { success: false, conflict: true, errors: { generics: ['Changed'] } };
    view.rerender(
      <MemoryRouter>
        <Edit />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Title*')).toHaveValue('Unsaved draft');
    expect(screen.getByRole('button', { name: 'Update' })).toBeDisabled();
    vi.mocked(getCalendarEventById).mockResolvedValue({
      ...event,
      title: 'Latest renamed title',
      revision: 2,
      categories: [],
    });
    fireEvent.click(screen.getByRole('button', { name: 'Reload Latest and Discard Draft' }));
    await waitFor(() =>
      expect(screen.getByLabelText('Title*')).toHaveValue('Latest renamed title')
    );
    expect(getCalendarEventById).toHaveBeenCalledWith(event.id);
    expect(screen.getByRole('button', { name: 'Update' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Update' }));
    expect((fetcher.submit.mock.calls[0][0] as FormData).get('revision')).toBe('2');
  });

  it('preserves the draft and conflict lock when reload fails', async () => {
    fetcher.data = { success: false, conflict: true };
    vi.mocked(getCalendarEventById).mockRejectedValue(new Error('Event no longer exists'));
    render(
      <MemoryRouter>
        <Edit />
      </MemoryRouter>
    );
    fireEvent.change(screen.getByLabelText('Title*'), { target: { value: 'Draft' } });
    fireEvent.click(screen.getByRole('button', { name: 'Reload Latest and Discard Draft' }));
    await screen.findByText('Event no longer exists');
    expect(screen.getByLabelText('Title*')).toHaveValue('Draft');
    expect(screen.getByRole('button', { name: 'Update' })).toBeDisabled();
  });

  it('disables repeat submission while pending', () => {
    fetcher.state = 'submitting';
    render(
      <MemoryRouter>
        <Edit />
      </MemoryRouter>
    );
    expect(screen.getByRole('button', { name: 'Updating...' })).toBeDisabled();
    expect(fetcher.submit).not.toHaveBeenCalled();
  });

  it('shows category loading failure without an unhandled rejection', async () => {
    vi.mocked(fetchCalendarCategories).mockRejectedValueOnce(new Error('Categories unavailable'));
    render(
      <MemoryRouter>
        <Create />
      </MemoryRouter>
    );
    await screen.findByText('Categories unavailable');
    await act(async () => {});
  });

  it.each([Detail, Index])(
    'deletes from the view using persisted identity and revision',
    async Component => {
      vi.mocked(useResolvedLoaderData).mockReturnValue(Component === Detail ? event : [event]);
      render(
        <MemoryRouter>
          <Component />
        </MemoryRouter>
      );
      fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
      fireEvent.click(screen.getAllByRole('button', { name: 'Delete' }).at(-1)!);
      expect(fetcher.submit).toHaveBeenCalledWith(
        { id: event.id, revision: '1' },
        expect.anything()
      );
    }
  );

  it.each([Detail, Index])(
    'notifies a successful delete once across toast context rerenders',
    Component => {
      const show = vi.fn();
      vi.mocked(useToast).mockImplementation(() => ({ show }) as never);
      vi.mocked(useResolvedLoaderData).mockReturnValue(Component === Detail ? event : [event]);
      fetcher.data = { success: true };
      const view = render(
        <MemoryRouter>
          <Component />
        </MemoryRouter>
      );
      view.rerender(
        <MemoryRouter>
          <Component />
        </MemoryRouter>
      );
      expect(show).toHaveBeenCalledTimes(1);
    }
  );

  it('offers recovery after stale delete without redirecting or claiming success', () => {
    const navigate = vi.fn();
    vi.mocked(useNavigate).mockReturnValue(navigate);
    vi.mocked(useResolvedLoaderData).mockReturnValue(event);
    fetcher.data = {
      success: false,
      conflict: true,
      errors: { generics: ['Event changed before deleting'] },
    };
    render(
      <MemoryRouter>
        <Detail />
      </MemoryRouter>
    );
    expect(navigate).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Return to Calendar Events' }));
    expect(navigate).toHaveBeenCalledWith('/calendarEvents');
  });
});
