import { data } from 'react-router';
import { Delete, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await Delete(ComposeUrl(endpoints.QUERYLINKS_DETAIL, { querylinkId: values.id as string }));
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false }, { status: 500 });
  }
}
