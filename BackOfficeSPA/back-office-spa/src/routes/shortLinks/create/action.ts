import { data } from 'react-router';

import { CreateShortLinkDTO, LinkType } from '@models';

export default async function action({ request }: { request: Request }) {
  const formData = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};
  
  // Convert form values to proper types
  const values: any = {
    target: String(formData.target || ''),
    linkType: Number(formData.linkType),
    queryString: String(formData.queryString || ''),
    message: String(formData.message || '')
  };
  
  // Validation
  if (!values.target || values.target.trim().length === 0) {
    errors['target'] = 'Target cannot be empty';
  }
  
  // For backward compatibility with the API
  values.videoId = values.target;

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // If no errors, proceed with API request
  return fetch(`/api/shortlinks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(() => {
      return data({ success: true }, { status: 201 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
