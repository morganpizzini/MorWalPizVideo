import { API_CONFIG } from '@config/api';
import { QueryLink } from '@models';

export default async function loader({ params }: { params: { id: string } }): Promise<QueryLink> {
  return fetch(`${API_CONFIG.BASE_URL}/querylinks/${params.id}`).then(response => response.json());
}
