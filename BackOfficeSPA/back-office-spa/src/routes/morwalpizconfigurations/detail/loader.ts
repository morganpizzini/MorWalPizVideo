
import { MorWalPizConfiguration } from '@/models/configuration';

export default async function loader({ params }: { params: { id: string } }): Promise<MorWalPizConfiguration> {
  return fetch(`/api/configurations/${params.id}`).then(response => response.json());
}
