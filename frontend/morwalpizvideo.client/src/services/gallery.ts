import { downloadPublicGallery, getPublicGallery } from '@morwalpizvideo/services';

const channelId =
  (typeof window !== 'undefined'
    ? window.ENV?.VITE_MORWALPIZ_CHANNEL_ID
    : import.meta.env.VITE_MORWALPIZ_CHANNEL_ID
  )?.trim() ?? '';

export function fetchPublicGallery(slug: string) {
  if (!channelId) {
    throw new Response('Gallery channel is not configured', { status: 503 });
  }

  return getPublicGallery(slug, channelId);
}

export function downloadGallery(slug: string, indexes?: number[]) {
  if (!channelId) {
    throw new Response('Gallery channel is not configured', { status: 503 });
  }

  return downloadPublicGallery(slug, channelId, indexes);
}
