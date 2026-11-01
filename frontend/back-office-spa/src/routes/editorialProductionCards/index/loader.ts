import { fetchEditorialProductionCards } from '@morwalpizvideo/services';
import type { EditorialProductionCard } from '@morwalpizvideo/models';

export default async function loader(): Promise<EditorialProductionCard[]> {
  const cards = await fetchEditorialProductionCards();
  if (!Array.isArray(cards))
    throw new Response('Unable to load editorial production cards', { status: 502 });
  return cards;
}
