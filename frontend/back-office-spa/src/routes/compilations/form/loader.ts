import { Compilation } from '@morwalpizvideo/models';

export default async function loader({ params }: { params: any }): Promise<Compilation | null> {
  // If no ID in params, this is create mode - return null
  if (!params.id) {
    return null;
  }

  // Edit mode - fetch existing compilation
  const response = await fetch(`/api/compilations/${params.id}`);
  if (!response.ok) {
    throw new Error('Failed to load compilation');
  }
  return response.json();
}
