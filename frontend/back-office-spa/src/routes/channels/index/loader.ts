import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  return get(endpoints.CHANNELS);
}
