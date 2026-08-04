import { get, endpoints,ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  try {
    if (!params.id) {
      throw new Response('Custom form not found', { status: 404 });
    }

    const [form, responses] = await Promise.all([
      get(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId: encodeURIComponent(params.id) })),
      get(ComposeUrl(endpoints.CUSTOMFORMS_RESPONSES, { customFormId: encodeURIComponent(params.id) }))
    ]);
    return { ...form, responses };
  } catch (error) {
    throw new Response('Custom form not found', { status: 404 });
  }
}
