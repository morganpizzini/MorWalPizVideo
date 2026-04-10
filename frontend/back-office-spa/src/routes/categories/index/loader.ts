import { Category } from '@morwalpizvideo/models';
import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader(): Promise<Category> {
    return get(endpoints.CATEGORIES)
}
