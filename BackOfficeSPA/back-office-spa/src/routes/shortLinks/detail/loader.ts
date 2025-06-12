
import { ShortLink } from '@models';

export default async function loader({ params }: { params: { id: string } }): Promise<ShortLink> {
  return fetch(`/api/shortlinks/${params.id}`).then(response => response.json());
}
