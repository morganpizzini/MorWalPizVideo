import { data } from 'react-router';
import { API_CONFIG } from '@config/api';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  return fetch(`${API_CONFIG.BASE_URL}/shortlinks/${values.id}`, { method: 'DELETE' })
    .then(() => {
      return data({ success: true }, { status: 201 });
    })
    .catch(() => {
      return data({ success: false, errors: ['Unexpected error found'] }, { status: 500 });
    });
}
