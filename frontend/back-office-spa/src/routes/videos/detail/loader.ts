import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }) {
  const [video, categories] = await Promise.all([
    get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id })),
    get(endpoints.CATEGORIES)
  ]);

  return { video, categories };
}
