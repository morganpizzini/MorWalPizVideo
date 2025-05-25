import { API_CONFIG } from '@config/api';
import { MorWalPizConfiguration } from '@/models/configuration';

export default async function loader(): Promise<MorWalPizConfiguration[]> {
  return fetch(`${API_CONFIG.BASE_URL}/configurations`).then(response => response.json());
}
