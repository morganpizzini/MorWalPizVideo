import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
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