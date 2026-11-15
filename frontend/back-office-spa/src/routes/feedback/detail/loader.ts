import type { LoaderFunctionArgs } from 'react-router';
import { getFeedback } from '@morwalpizvideo/services';

export default function loader({ params }: LoaderFunctionArgs) {
  if (!params.id) throw new Response('Feedback id is required', { status: 400 });
  return getFeedback(params.id);
}
