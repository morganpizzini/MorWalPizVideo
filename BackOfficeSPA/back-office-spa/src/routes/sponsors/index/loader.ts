import { fetchSponsors } from '@services/apiService';

export async function loader() {
  const sponsors = await fetchSponsors();
  return { sponsors };
}
