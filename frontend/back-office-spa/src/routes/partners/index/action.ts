import { data } from 'react-router';
import { deletePartner } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  try {
    await deletePartner(String(values.id));
    return data({ success: true }, { status: 200 });
  } catch {
    return data(
      { success: false, errors: { generics: ['Unable to delete the partner.'] } },
      { status: 409 }
    );
  }
}
