import { QueryLink } from '@models';
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }): Promise<QueryLink> {
    return get(ComposeUrl(endpoints.QUERYLINKS_DETAIL, { querylinkId: params.id })).then(response => ({
        ...response,
        breadcrumbIdentifier: response.title
    }))
}
