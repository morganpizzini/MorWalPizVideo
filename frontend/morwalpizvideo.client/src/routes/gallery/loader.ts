import type { LoaderFunctionArgs } from 'react-router';
import { ApiResponseError } from '@morwalpizvideo/services';
import { fetchPublicGallery } from '../../services/gallery';

export const GALLERY_UNAVAILABLE_MESSAGE =
  'This gallery has expired or is no longer available.';

export default async function loader({ params }: LoaderFunctionArgs) {
  const slug = params.slug?.trim();
  if (!slug) throw new Response(GALLERY_UNAVAILABLE_MESSAGE, {
    status: 404,
    statusText: GALLERY_UNAVAILABLE_MESSAGE,
  });

  try {
    return await fetchPublicGallery(slug);
  } catch (error) {
    if (error instanceof ApiResponseError && error.status === 404)
      throw new Response(GALLERY_UNAVAILABLE_MESSAGE, {
        status: 404,
        statusText: GALLERY_UNAVAILABLE_MESSAGE,
      });
    throw error;
  }
}
