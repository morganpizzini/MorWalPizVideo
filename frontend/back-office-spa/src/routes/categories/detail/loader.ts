import { Category } from '@morwalpizvideo/models';
import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { id: string } }): Promise<Category> {
    return get(ComposeUrl(endpoints.CATEGORIES_DETAIL,{categoryId:params.id})).then(response => ({
        ...response,
        breadcrumbIdentifier: response.title
    }))
}
