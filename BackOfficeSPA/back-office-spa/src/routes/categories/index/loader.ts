
import { Category } from '@models';

export default async function loader(): Promise<any> {
  return fetch(`/api/categories`);
}
