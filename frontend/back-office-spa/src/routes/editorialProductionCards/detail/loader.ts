import { getEditorialProductionCard } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  if (!params.id) throw new Response('Card ID is required', { status: 400 });
  return getEditorialProductionCard(params.id);
}
