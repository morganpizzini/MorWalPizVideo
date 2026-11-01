import { getEditorialProductionCard } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  return params.id ? getEditorialProductionCard(params.id) : null;
}
