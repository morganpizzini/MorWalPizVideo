import { ShortLink } from '@morwalpizvideo/models';
import { get, endpoints } from '@morwalpizvideo/services';
export default async function loader(): Promise<ShortLink[]> {
    return get(endpoints.SHORTLINKS)
}
