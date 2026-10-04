import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const shouldShowPushPrompt = vi.fn();
const dismissPushPrompt = vi.fn();
const requestPushOptIn = vi.fn();
const loadPushChannelOptions = vi.fn();

vi.mock('@morwalpizvideo/services', () => ({
  shouldShowPushPrompt: () => shouldShowPushPrompt(),
  dismissPushPrompt: () => dismissPushPrompt(),
  requestPushOptIn: (options: unknown) => requestPushOptIn(options),
}));

vi.mock('../services/push', () => ({
  PUSH_APPLICATION_KEY: 'shooting-ita',
  loadPushChannelOptions: () => loadPushChannelOptions(),
}));

import PushOptIn from '../components/PushOptIn';

function renderPrompt() {
  return render(
    <MemoryRouter>
      <PushOptIn />
    </MemoryRouter>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  shouldShowPushPrompt.mockReturnValue(true);
  loadPushChannelOptions.mockResolvedValue([
    { channelId: 'channel-1', channelName: 'Shooting ITA' },
  ]);
  requestPushOptIn.mockResolvedValue({ status: 'subscribed' });
});

describe('PushOptIn', () => {
  it('stays hidden when the visitor already decided', async () => {
    shouldShowPushPrompt.mockReturnValue(false);
    renderPrompt();
    await waitFor(() => expect(loadPushChannelOptions).not.toHaveBeenCalled());
    expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
  });

  it('subscribes to every available channel and confirms', async () => {
    renderPrompt();
    await userEvent.click(await screen.findByRole('button', { name: 'Attiva' }));

    expect(requestPushOptIn).toHaveBeenCalledWith({
      applicationKey: 'shooting-ita',
      channelIds: ['channel-1'],
      language: 'IT',
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Notifiche attivate');
  });

  it('records the dismissal and removes itself', async () => {
    renderPrompt();
    await userEvent.click(await screen.findByRole('button', { name: 'No, grazie' }));

    expect(dismissPushPrompt).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
  });

  it('surfaces a recoverable error when the browser blocks the permission', async () => {
    requestPushOptIn.mockResolvedValue({ status: 'denied' });
    renderPrompt();
    await userEvent.click(await screen.findByRole('button', { name: 'Attiva' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Non è stato possibile attivare le notifiche',
    );
  });
});
