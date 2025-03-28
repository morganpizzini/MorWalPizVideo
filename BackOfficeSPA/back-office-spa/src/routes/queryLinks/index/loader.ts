import { API_CONFIG } from '@config/api';
import { QueryLink } from '@models';

export default async function loader(): Promise<QueryLink[]> {
  return fetch(`${API_CONFIG.BASE_URL}/querylinks`).then(response => response.json());
}
