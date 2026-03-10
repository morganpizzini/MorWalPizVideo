import { Compilation } from '@morwalpizvideo/models';

export default async function loader({ params }: { params: any }): Promise<Compilation> {
  const response = await fetch(`/api/compilations/${params.id}`);
  if (!response.ok) {
    throw new Error('Failed to load compilation');
  }
  return response.json();
}
