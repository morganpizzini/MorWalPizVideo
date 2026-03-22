import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  try {
    const response = await get(endpoints.CUSTOMFORMS);
    return response;
  } catch (error) {
    return [];
  }
}
