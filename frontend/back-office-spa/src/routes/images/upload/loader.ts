import { get, endpoints } from '@morwalpizvideo/services';
import type { Match } from '@morwalpizvideo/models';

export default async function loader(): Promise<{ matches: Match[] }> {
  const matches = await get(endpoints.VIDEOS) as Match[];
  return { matches };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
