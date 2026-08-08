import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';
import type { Channel } from '@morwalpizvideo/models';
import { requireChannelPayload } from '../response';

export default async function loader({ params }: LoaderFunctionArgs) {
  const response = await get(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: params.id! }));
  return requireChannelPayload<Channel>(response, 'Channel not found');
}
