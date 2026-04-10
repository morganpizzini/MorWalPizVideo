import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  const categories = await get(endpoints.CATEGORIES);
  return { categories };
}
