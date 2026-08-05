import { get, endpoints } from '@morwalpizvideo/services';
import type { Match } from '@morwalpizvideo/models';

export default async function loader(): Promise<{ matches: Match[] }> {
  try {
    const matches = await get(endpoints.VIDEOS) as Match[];
    return { matches };
  } catch (error) {
    return { matches: [] };
  }
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
