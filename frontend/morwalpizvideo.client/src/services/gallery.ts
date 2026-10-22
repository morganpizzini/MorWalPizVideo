import { downloadPublicGallery, getPublicGallery } from '@morwalpizvideo/services';

export function fetchPublicGallery(slug: string) {
 
  return getPublicGallery(slug, '');
}

export function downloadGallery(slug: string, indexes?: number[]) {
  return downloadPublicGallery(slug, '', indexes);
}
