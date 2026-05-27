import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { id?: string } }) {
  if (params.id) {
    return get(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: params.id }));
  }
  return null;
}
