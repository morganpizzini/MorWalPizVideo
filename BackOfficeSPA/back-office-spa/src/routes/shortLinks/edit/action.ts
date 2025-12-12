import { data } from 'react-router';

import { ActionFunctionArgs } from 'react-router';
import { UpdateShortLinkDTO } from '@/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  // Convert form values to proper types
  const values: any = {
    shortLinkId: String(formData.shortLinkId || ''),
    target: String(formData.target || ''),
    linkType: Number(formData.linkType),
    queryLinkIds: formData.queryLinkIds ? JSON.parse(String(formData.queryLinkIds)) : [],
    message: String(formData.message || ''),
    clicksCount: String(formData.clicksCount || ''),
  };

  // Field validation
  if (!values.target || values.target.trim().length === 0) {
    errors['target'] = 'Target cannot be empty';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // API request
  return fetch(`/api/shortlinks/${params.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(() => {
      return data({ success: true }, { status: 200 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
