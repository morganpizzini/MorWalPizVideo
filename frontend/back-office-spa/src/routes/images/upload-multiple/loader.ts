import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  try {
    const matches = await get(endpoints.VIDEOS);
    return { matches };
  } catch (error) {
    return { matches: [] };
  }
}
