import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { id?: string } }) {
  if (params.id) {
    try {
      const response = await get(ComposeUrl(endpoints.APIKEYS_DETAIL, { id: encodeURIComponent(params.id) }));
      return response;
    } catch (error) {
      throw new Response('API Key not found', { status: 404 });
    }
  }
  return null;
}