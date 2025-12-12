import { getSponsor } from '@services/apiService';

export async function loader({ params }: { params: { id: string } }) {
  const sponsor = await getSponsor(params.id);
  return sponsor;
}
