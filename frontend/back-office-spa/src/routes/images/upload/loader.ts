import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  const matches = await get(endpoints.VIDEOS);
  return { matches };
}
