import { QueryLink } from '@morwalpizvideo/models';
import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs): Promise<QueryLink> {
    return get(ComposeUrl(endpoints.QUERYLINKS_DETAIL, { querylinkId: params.id! })).then(response => ({
        ...response,
        breadcrumbIdentifier: response.title
    }))
}
