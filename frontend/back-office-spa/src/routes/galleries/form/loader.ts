import { getGallery } from '@morwalpizvideo/services';

export default function loader({ params }: { params: { id?: string } }) {
  return params.id ? getGallery(params.id) : null;
}
