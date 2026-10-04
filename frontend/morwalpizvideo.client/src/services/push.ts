/** Application-level push configuration for the public MorWalPiz client. */

export const PUSH_APPLICATION_KEY = 'morwalpizvideo';

/**
 * The public client subscribes to its own configured channel. Runtime `window.ENV` wins over the build-time value so
 * a single image can serve several deployments.
 */
export function getPushChannelIds(): string[] {
  const channelId = (
    typeof window !== 'undefined'
      ? window.ENV?.VITE_MORWALPIZ_CHANNEL_ID
      : import.meta.env.VITE_MORWALPIZ_CHANNEL_ID
  )?.trim();
  return channelId ? [channelId] : [];
}
