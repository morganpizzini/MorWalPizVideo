import { API_CONFIG } from '@config/api';
import { ShortLink } from '@models';

export default async function loader({ params }: { params: { id: string } }): Promise<ShortLink> {
  return fetch(`${API_CONFIG.BASE_URL}/shortlinks/${params.id}`).then(response => response.json());
}
