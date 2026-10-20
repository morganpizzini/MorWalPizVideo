import { getGallery } from '@morwalpizvideo/services';

export default function loader({ params }: { params: { id?: string } }) {
  if (!params.id) throw new Response('Gallery ID is required', { status: 400 });
  return getGallery(params.id);
}
