import { Compilation } from '@morwalpizvideo/models';

export default async function loader(): Promise<Compilation[]> {
  const response = await fetch('/api/compilations');
  if (!response.ok) {
    throw new Error('Failed to load compilations');
  }
  return response.json();
}
