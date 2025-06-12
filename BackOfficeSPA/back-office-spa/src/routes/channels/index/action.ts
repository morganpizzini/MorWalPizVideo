import { data } from 'react-router';


export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  return fetch(`/api/channels/${values.id}`, { method: 'DELETE' })
    .then(() => {
      return data({ success: true }, { status: 201 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
