import { API_CONFIG } from '@config/api';
import { MorWalPizConfiguration } from '@/models/configuration';

export default async function loader({ params }: { params: { id: string } }): Promise<MorWalPizConfiguration> {
  return fetch(`${API_CONFIG.BASE_URL}/configurations/${params.id}`).then(response => response.json());
}
