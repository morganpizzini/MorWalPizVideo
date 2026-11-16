import { getPublicAccountInteractions } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: Record<string, string | undefined> }) {
  if (!params.id) throw new Error('Account ID is required.');
  return getPublicAccountInteractions(params.id);
}
