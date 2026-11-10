import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { useResolvedLoaderData } from '@/router/asyncData';
import { useChannelContext } from '@/contexts/ChannelContext';
import { render } from '../../../test/test-utils';
import Component from './Component';

vi.mock('@/router/asyncData', () => ({ useResolvedLoaderData: vi.fn() }));
vi.mock('@/contexts/ChannelContext', () => ({ useChannelContext: vi.fn() }));

const channel = {
  channelId: 'owner',
  channelName: 'Owner',
  yTChannelId: 'UCONE',
  publicBaseUrl: 'https://example.com/',
};
const context = {
  channels: [channel],
  selectedChannelId: 'owner',
  selectChannel: vi.fn(),
  channelSwitchLocked: false,
  setChannelSwitchLocked: vi.fn(),
};

beforeEach(() => {
  vi.mocked(useResolvedLoaderData).mockReturnValue({
    id: 'page',
    channelId: 'owner',
    title: 'Community',
    url: 'community',
    links: [],
  });
  vi.mocked(useChannelContext).mockReturnValue(context);
});

describe('QuickLinks public destination', () => {
  it('renders the exact absolute destination with safe new-tab behavior', () => {
    render(<Component />);
    const anchor = screen.getByRole('link', { name: 'https://example.com/quick-links/community' });
    expect(anchor).toHaveAttribute('href', 'https://example.com/quick-links/community');
    expect(anchor).toHaveAttribute('target', '_blank');
    expect(anchor).toHaveAttribute('rel', 'noopener noreferrer');
    expect(anchor.closest('p')).toHaveClass('text-break');
  });

  it.each([
    ['', 'Configure the channel'],
    ['javascript:alert(1)', 'is invalid'],
    ['https://user:pass@example.com', 'is invalid'],
    ['https://example.com/path', 'is invalid'],
  ])('does not make missing or invalid configuration clickable: %s', (publicBaseUrl, message) => {
    vi.mocked(useChannelContext).mockReturnValue({
      ...context,
      channels: [{ ...channel, publicBaseUrl }],
    });
    render(<Component />);
    expect(screen.getByText(new RegExp(message))).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /quick-links/ })).not.toBeInTheDocument();
  });

  it.each([
    [null, 'Select an accessible channel'],
    ['other', 'Select the owning channel'],
  ])('requires an accessible selected owner: %s', (selectedChannelId, message) => {
    vi.mocked(useChannelContext).mockReturnValue({
      ...context,
      selectedChannelId,
      channels: [
        channel,
        { ...channel, channelId: 'other', publicBaseUrl: 'https://other.example' },
      ],
    });
    render(<Component />);
    expect(screen.getByText(new RegExp(message))).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /quick-links/ })).not.toBeInTheDocument();
  });

  it('does not infer ownership for legacy pages', () => {
    vi.mocked(useResolvedLoaderData).mockReturnValue({ title: 'Legacy', url: 'legacy', links: [] });
    render(<Component />);
    expect(screen.getByText(/no channel owner/)).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /quick-links/ })).not.toBeInTheDocument();
  });
});
