import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { id?: string } }) {
  if (params.id) {
    try {
      const response = await get(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId: encodeURIComponent(params.id) }));
      return response;
    } catch (error) {
      throw new Response('Custom form not found', { status: 404 });
    }
  }
  return null;
}
