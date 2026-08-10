import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  const [video, categories, channelsResult] = await Promise.all([
    get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id! })),
    get(endpoints.CATEGORIES),
    get(endpoints.CHANNELS_ACCESSIBLE).then(value => ({ value, error: undefined })).catch(error => ({ value: [], error }))
  ]);

  if (channelsResult.error) {
    console.warn('Accessible channel metadata unavailable while loading video detail.', channelsResult.error);
  }

  return { match: video, categories, channels: channelsResult.value };
}
