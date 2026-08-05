import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  const [video, categories] = await Promise.all([
    get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id! })),
    get(endpoints.CATEGORIES)
  ]);

  return { match: video, categories };
}
