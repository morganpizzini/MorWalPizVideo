import { fireEvent, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { get, getSelectedChannelId, setSelectedChannelId } from '@morwalpizvideo/services';
import { useRevalidator } from 'react-router';
import { render } from '../test/test-utils';
import { ChannelProvider, useChannelContext } from './ChannelContext';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useRevalidator: vi.fn(),
  };
});

const channels = [
  { channelId: 'channel-one', channelName: 'One', yTChannelId: 'UCONE', mine: true },
  { channelId: 'channel-two', channelName: 'Two', yTChannelId: 'UCTWO', mine: true },
];

function SelectionProbe() {
  const { selectedChannelId, selectChannel } = useChannelContext();
  return (
    <>
      <output data-testid="selected-channel">{selectedChannelId ?? ''}</output>
      <button type="button" onClick={() => selectChannel('channel-two')}>Switch</button>
    </>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  setSelectedChannelId(null);
  vi.mocked(useRevalidator).mockReturnValue({
    state: 'idle',
    revalidate: vi.fn(),
  } as never);
});

describe('ChannelContext', () => {
  it('clears a stale selection when no accessible channels remain', () => {
    setSelectedChannelId('stale-channel');

    render(
      <ChannelProvider channels={[]}>
        <SelectionProbe />
      </ChannelProvider>
    );

    expect(screen.getByTestId('selected-channel')).toHaveTextContent('');
    expect(getSelectedChannelId()).toBeNull();
  });

  it('switches the selected channel and revalidates scoped routes', () => {
    const revalidate = vi.fn();
    vi.mocked(useRevalidator).mockReturnValue({ state: 'idle', revalidate } as never);

    render(
      <ChannelProvider channels={channels}>
        <SelectionProbe />
      </ChannelProvider>
    );

    fireEvent.click(screen.getByRole('button', { name: 'Switch' }));

    expect(screen.getByTestId('selected-channel')).toHaveTextContent('channel-two');
    expect(getSelectedChannelId()).toBe('channel-two');
    expect(revalidate).toHaveBeenCalledTimes(1);
  });
});

describe('shared channel API client', () => {
  it('propagates the selected channel header to scoped requests', async () => {
    setSelectedChannelId('channel-two');
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [],
    } as Response);

    await get('/api/videos');

    const requestInit = fetchMock.mock.calls[0]?.[1];
    const headers = new Headers(requestInit?.headers);
    expect(headers.get('X-Channel-Id')).toBe('channel-two');

    fetchMock.mockRestore();
  });
});