import { downloadPublicGallery, getPublicGallery } from '@morwalpizvideo/services';

const channelId =
  (typeof window !== 'undefined'
    ? window.ENV?.VITE_MORWALPIZ_CHANNEL_ID
    : import.meta.env.VITE_MORWALPIZ_CHANNEL_ID
  )?.trim() ?? '';

export function fetchPublicGallery(slug: string) {
 
  return getPublicGallery(slug, '');
}

export function downloadGallery(slug: string, indexes?: number[]) {
  return downloadPublicGallery(slug, '', indexes);
}
