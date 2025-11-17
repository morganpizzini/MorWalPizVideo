import { ShortLink } from '@models';
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader(): Promise<ShortLink[]> {
    return get(endpoints.SHORTLINKS)
}
