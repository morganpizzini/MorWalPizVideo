import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }) {
  return get(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId: params.id }));
}
