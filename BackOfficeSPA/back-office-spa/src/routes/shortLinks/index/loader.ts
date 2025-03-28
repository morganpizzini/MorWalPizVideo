import { API_CONFIG } from '@config/api';
import { ShortLink } from '@models';

export default async function loader(): Promise<ShortLink[]> {
  return fetch(`${API_CONFIG.BASE_URL}/shortlinks`).then(response => response.json());
}
