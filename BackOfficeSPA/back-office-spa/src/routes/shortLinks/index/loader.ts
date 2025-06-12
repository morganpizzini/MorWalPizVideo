
import { ShortLink } from '@models';

export default async function loader(): Promise<ShortLink[]> {
  return fetch(`/api/shortlinks`).then(response => response.json());
}
