import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { PushTargets } from '@morwalpizvideo/models';
import { render } from '../../../test/test-utils';

const sendPushPlatform = vi.fn();
const sendPushChannel = vi.fn();
const createPushNotificationTemplate = vi.fn();

vi.mock('@morwalpizvideo/services', () => ({
  sendPushPlatform: (request: unknown) => sendPushPlatform(request),
  sendPushChannel: (request: unknown) => sendPushChannel(request),
  createPushAudience: vi.fn(),
  deletePushAudience: vi.fn(),
  getPushTargets: vi.fn(),
  fetchPushNotificationTemplates: vi.fn().mockResolvedValue([]),
  createPushNotificationTemplate,
  updatePushNotificationTemplate: vi.fn(),
  deletePushNotificationTemplate: vi.fn(),
}));

const targets: PushTargets = {
  channels: [
    { channelId: 'channel-1', channelName: 'Primary', activeSubscriberCount: 12 },
    { channelId: 'channel-2', channelName: 'Secondary', activeSubscriberCount: 3 },
  ],
  audiences: [
    {
      id: 'audience-1',
      code: 'creators',
      name: 'Creators',
      description: '',
      channelIds: ['channel-1'],
      isActive: true,
      creationDateTime: '2026-01-01T00:00:00Z',
    },
  ],
  maxActions: 2,
};

let effectivePermissions: string[] = [];

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: () => targets,
}));

vi.mock('@/state/appStore', () => ({
  useAppStore: (selector: (state: { effectivePermissions: string[] }) => unknown) =>
    selector({ effectivePermissions }),
}));

async function renderPush() {
  const { default: Push } = await import('../Component');
  return render(<Push />);
}

beforeEach(() => {
  vi.clearAllMocks();
  effectivePermissions = ['push.platform.send'];
  sendPushPlatform.mockResolvedValue({ recipientCount: 15 });
  sendPushChannel.mockResolvedValue({ recipientCount: 12 });
  createPushNotificationTemplate.mockResolvedValue({ id: 'template-1', name: 'Release', title: 'Release', body: 'New video', destination: '/', actions: [], version: 1, isActive: true, updatedAt: '' });
});

describe('Push notifications', () => {
  it('hides the platform section without push.platform.send', async () => {
    effectivePermissions = ['backoffice.access'];
    await renderPush();

    expect(screen.queryByText('Platform broadcast')).not.toBeInTheDocument();
    expect(screen.getByText('Channel broadcast')).toBeInTheDocument();
  });

  it('selects every channel at once and sends with the all-channels flag', async () => {
    await renderPush();

    await userEvent.click(screen.getByLabelText('Select all channels'));
    await userEvent.type(screen.getByLabelText('Title', { selector: '#platform-title' }), 'Release');
    await userEvent.type(
      screen.getByLabelText('Body', { selector: '#platform-body' }),
      'A new video is out.'
    );
    await userEvent.click(screen.getByRole('button', { name: 'Send to platform' }));

    expect(sendPushPlatform).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Release',
        body: 'A new video is out.',
        channelIds: ['channel-1', 'channel-2'],
        allChannels: true,
      })
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Queued for 15 subscription(s)');
  });

  it('targets a named audience without selecting channels', async () => {
    await renderPush();

    await userEvent.click(screen.getByLabelText('Creators (1 channels)'));
    await userEvent.type(
      screen.getByLabelText('Title', { selector: '#platform-title' }),
      'Audience send'
    );
    await userEvent.type(screen.getByLabelText('Body', { selector: '#platform-body' }), 'Targeted.');
    await userEvent.click(screen.getByRole('button', { name: 'Send to platform' }));

    expect(sendPushPlatform).toHaveBeenCalledWith(
      expect.objectContaining({ audienceIds: ['audience-1'], channelIds: [] })
    );
  });

  it('stops adding action buttons at the server maximum', async () => {
    await renderPush();
    const addAction = screen.getByRole('button', { name: 'Add platform action' });

    await userEvent.click(addAction);
    await userEvent.click(addAction);

    expect(addAction).toBeDisabled();
    expect(screen.getByLabelText('platform action 1 id')).toBeInTheDocument();
    expect(screen.getByLabelText('platform action 2 id')).toBeInTheDocument();
    expect(screen.queryByLabelText('platform action 3 id')).not.toBeInTheDocument();
  });

  it('broadcasts to the managed channel without a recipient picker', async () => {
    effectivePermissions = ['backoffice.access'];
    await renderPush();

    await userEvent.type(screen.getByLabelText('Title'), 'Channel news');
    await userEvent.type(screen.getByLabelText('Body'), 'For my subscribers.');
    await userEvent.click(screen.getByRole('button', { name: 'Send to my subscribers' }));

    expect(sendPushChannel).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Channel news', body: 'For my subscribers.' })
    );
  });

  it('creates a reusable template from the editor', async () => {
    await renderPush();
    await userEvent.type(screen.getByLabelText('Name', { selector: '#template-name' }), 'Release');
    await userEvent.type(screen.getByLabelText('Title', { selector: '#template-title' }), 'Release');
    await userEvent.type(screen.getByLabelText('Body', { selector: '#template-body' }), 'New video');
    await userEvent.click(screen.getByRole('button', { name: 'Add action' }));
    await userEvent.type(screen.getByLabelText('Template action 1 id'), 'open');
    await userEvent.type(screen.getByLabelText('Template action 1 label'), 'Open');
    await userEvent.click(screen.getByRole('button', { name: 'Create template' }));
    expect(createPushNotificationTemplate).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Release', title: 'Release', body: 'New video',
      actions: [expect.objectContaining({ action: 'open', title: 'Open' })],
    }));
    expect((await screen.findAllByText('Release')).length).toBeGreaterThan(0);
  });
});
