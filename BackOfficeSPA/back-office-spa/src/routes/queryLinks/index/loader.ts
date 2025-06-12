
import { QueryLink } from '@models';

export default async function loader(): Promise<QueryLink[]> {
  return fetch(`/api/querylinks`).then(response => response.json());
}
