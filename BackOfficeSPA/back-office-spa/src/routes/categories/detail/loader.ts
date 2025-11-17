import { Category } from '@models';
import { get } from '@services/apiService';
import endpoints, { ComposeUrl} from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }): Promise<Category> {
    return get(ComposeUrl(endpoints.CATEGORIES_DETAIL,{categoryId:params.id})).then(response => ({
        ...response,
        breadcrumbIdentifier: response.title
    }))
}
