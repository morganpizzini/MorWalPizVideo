import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { id: string } }) {
  const [video, categories] = await Promise.all([
    get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id })),
    get(endpoints.CATEGORIES)
  ]);

  return { match: video, categories };
}
