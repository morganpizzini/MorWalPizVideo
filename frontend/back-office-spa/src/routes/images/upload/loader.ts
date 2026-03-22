import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const matches = await get(endpoints.VIDEOS);
  return { matches };
}
