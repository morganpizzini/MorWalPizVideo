import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  const [categories, videos] = await Promise.all([
    get(endpoints.CATEGORIES),
    get(endpoints.VIDEOS)
  ]);

  return { categories, videos };
}
