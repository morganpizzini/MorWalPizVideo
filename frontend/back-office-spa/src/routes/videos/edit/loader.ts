import { LoaderFunctionArgs } from 'react-router';
import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: LoaderFunctionArgs) {
  const match = await get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id! }));
  const categories = await get(endpoints.CATEGORIES);
  // Bounded, channel-scoped suggestion pool; filtered client-side while typing.
  const tagSuggestions = await get(endpoints.VIDEOS_TAG_SUGGESTIONS).catch(() => [] as string[]);

  return { match, categories, tagSuggestions };
}