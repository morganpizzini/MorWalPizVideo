import { get, endpoints } from '@morwalpizvideo/services';
import type { Match } from '@morwalpizvideo/models';

export default async function loader(): Promise<{ videos: Match[] }> {
  const videos = await get(endpoints.VIDEOS) as Match[];
  return { videos };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
