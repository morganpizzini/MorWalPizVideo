import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

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
