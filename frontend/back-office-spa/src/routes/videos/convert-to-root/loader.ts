import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const [categories, videos] = await Promise.all([
    get(endpoints.CATEGORIES),
    get(endpoints.VIDEOS)
  ]);

  return { categories, videos };
}
