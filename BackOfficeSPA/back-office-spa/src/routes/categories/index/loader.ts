import { Category } from '@models';
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader(): Promise<Category> {
    return get(endpoints.CATEGORIES)
}
