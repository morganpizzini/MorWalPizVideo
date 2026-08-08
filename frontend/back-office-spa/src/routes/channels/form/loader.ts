import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { Channel } from '@morwalpizvideo/models';
import { requireChannelPayload } from '../response';

export default async function loader({ params }: { params: { id?: string } }) {
  if (params.id) {
    const response = await get(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: params.id }));
    return requireChannelPayload<Channel>(response, 'Channel not found');
  }
  return null;
}
