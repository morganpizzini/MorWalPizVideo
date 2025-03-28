import { API_CONFIG } from '@config/api';

export default async function loader() {
  return fetch(`${API_CONFIG.BASE_URL}/channels`).then(response => response.json());
}
