import { get, endpoints } from '@morwalpizvideo/services';
import type { Category, Match } from '@morwalpizvideo/models';

export default async function loader(): Promise<{ categories: Category[]; videos: Match[] }> {
  const [categories, videos] = await Promise.all([
    get(endpoints.CATEGORIES) as Promise<Category[]>,
    get(endpoints.VIDEOS) as Promise<Match[]>
  ]);

  return { categories, videos };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
