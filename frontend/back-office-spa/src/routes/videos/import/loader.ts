import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const categories = await get(endpoints.CATEGORIES);
  return { categories };
}
