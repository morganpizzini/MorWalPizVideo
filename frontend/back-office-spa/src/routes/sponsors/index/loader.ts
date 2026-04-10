import { fetchSponsors } from '@morwalpizvideo/services';

export async function loader() {
  const sponsors = await fetchSponsors();
  return { sponsors };
}
