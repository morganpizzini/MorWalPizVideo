import type { LoaderFunctionArgs } from 'react-router';
import { getCompilationByUrl } from '@services/compilations';

export default async function loader({ params }: LoaderFunctionArgs) {
  const compilationUrl = params.compilationUrl?.trim();
  if (!compilationUrl) {
    throw new Response('Compilation not found', { status: 404 });
  }

  const compilation = await getCompilationByUrl(compilationUrl);
  return { compilation };
}
