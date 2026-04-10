import { QueryLink } from '@morwalpizvideo/models';
import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { id: string } }): Promise<QueryLink> {
    return get(ComposeUrl(endpoints.QUERYLINKS_DETAIL, { querylinkId: params.id })).then(response => ({
        ...response,
        breadcrumbIdentifier: response.title
    }))
}
