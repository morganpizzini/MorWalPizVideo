import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const videos = await get(endpoints.VIDEOS);
  return { videos };
}
