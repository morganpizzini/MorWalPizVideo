
import { ShortLink } from '@morwalpizvideo/models';

export default async function loader({ params }: { params: { id: string } }): Promise<ShortLink> {
  return fetch(`/api/shortlinks/${params.id}`).then(response => response.json());
}
