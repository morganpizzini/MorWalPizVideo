import { API_CONFIG } from '@config/api';
import { Category } from '@models';

export default async function loader({ params }: { params: { id: string } }): Promise<Category> {
  return fetch(`${API_CONFIG.BASE_URL}/categories/${params.id}`).then(response => response.json());
}
