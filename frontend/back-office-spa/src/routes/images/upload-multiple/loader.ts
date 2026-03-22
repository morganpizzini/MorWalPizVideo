import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  try {
    const matches = await get(endpoints.VIDEOS);
    return { matches };
  } catch (error) {
    return { matches: [] };
  }
}
