import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  try {
    const id = params.id;
    if (!id) {
      throw new Error('API Key ID is required');
    }

    const response = await get(
      ComposeUrl(endpoints.APIKEYS_DETAIL, { id: encodeURIComponent(id) })
    );
    return response;
  } catch (error) {
    throw new Response('API Key not found', { status: 404 });
  }
}