import { API_CONFIG } from '@config/api';

export default async function loader({ params }: { params: { id: string } }) {
  return fetch(`${API_CONFIG.BASE_URL}/channels/${params.id}`).then(response => response.json());
}
