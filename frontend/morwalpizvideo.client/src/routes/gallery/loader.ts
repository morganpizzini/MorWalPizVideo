import type { LoaderFunctionArgs } from 'react-router';
import { fetchPublicGallery } from '../../services/gallery';

export default function loader({ params }: LoaderFunctionArgs) {
  const slug = params.slug?.trim();
  if (!slug) throw new Response('Gallery not found', { status: 404 });

  return fetchPublicGallery(slug);
}
