import { API_CONFIG } from '@config/api';
import { Category } from '@models';

export default async function loader(): Promise<Category[]> {
  return fetch(`${API_CONFIG.BASE_URL}/categories`).then(response => response.json());
}
