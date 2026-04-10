import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  const videos = await get(endpoints.VIDEOS);
  return { videos };
}
