import { Compilation } from '@morwalpizvideo/models';
import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: any }): Promise<Compilation> {
  return get(ComposeUrl(endpoints.COMPILATIONS_DETAIL, { compilationId: params.id }));
}
