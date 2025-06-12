
import { QueryLink } from '@models';

export default async function loader({ params }: { params: { id: string } }): Promise<QueryLink> {
  return fetch(`/api/querylinks/${params.id}`).then(response => response.json());
}
