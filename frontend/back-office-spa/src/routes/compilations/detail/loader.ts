import { Compilation } from '@morwalpizvideo/models';
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: any }): Promise<Compilation> {
  return get(ComposeUrl(endpoints.COMPILATIONS_DETAIL, { compilationId: params.id }));
}
