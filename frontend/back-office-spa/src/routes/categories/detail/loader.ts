import { Category } from '@morwalpizvideo/models';
import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs): Promise<Category> {
    return get(ComposeUrl(endpoints.CATEGORIES_DETAIL,{categoryId:params.id!})).then(response => ({
        ...response,
        breadcrumbIdentifier: response.title
    }))
}
