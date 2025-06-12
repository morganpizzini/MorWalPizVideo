
import { MorWalPizConfiguration } from '@/models/configuration';

export default async function loader(): Promise<MorWalPizConfiguration[]> {
  return fetch(`/api/configurations`).then(response => response.json());
}
