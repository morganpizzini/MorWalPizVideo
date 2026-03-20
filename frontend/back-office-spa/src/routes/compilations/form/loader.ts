import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';
import { Compilation } from '@morwalpizvideo/models';

export default async function loader({ params }: { params: any }): Promise<Compilation | null> {
  // If no ID in params, this is create mode - return null
  if (!params.id) {
    return null;
  }

  // Edit mode - fetch existing compilation
  return get(ComposeUrl(endpoints.COMPILATIONS_DETAIL, { compilationId: params.id }));
}
