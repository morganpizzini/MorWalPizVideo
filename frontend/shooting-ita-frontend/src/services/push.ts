/** Application-level push configuration for the Shooting ITA client. */

import {
  frontendEndpoints,
  publicGet as get,
  type ChannelWithVideos,
} from '@morwalpizvideo/services';

export const PUSH_APPLICATION_KEY = 'shooting-ita';
export function isPushEnabled(): boolean {
  if (import.meta.env.PROD && !import.meta.env.VITE_SHOOTING_ITA_PUSH_ORIGIN) return false;
  return true;
}

export interface PushChannelOption {
  channelId: string;
  channelName: string;
}

/**
 * Shooting ITA aggregates several channels, so the opt-in is per channel. The selectable set comes from the same
 * public endpoint that drives the video feeds, which keeps the two in step without a second source of truth.
 */
export async function loadPushChannelOptions(): Promise<PushChannelOption[]> {
  const channels = (await get(frontendEndpoints.SHIT_CHANNELS)) as
    | ChannelWithVideos[]
    | undefined;
  return (channels ?? []).map(channel => ({
    channelId: channel.channelId,
    channelName: channel.channelName,
  }));
}
