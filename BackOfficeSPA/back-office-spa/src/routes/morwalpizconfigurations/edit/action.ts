import { ActionFunctionArgs, data } from 'react-router';
import { API_CONFIG } from '@config/api';
import { UpdateConfigurationDTO } from '@/models/configuration';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData()) as UpdateConfigurationDTO;
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!values.key || values.key.trim().length === 0) {
    errors['key'] = 'Key cannot be empty';
  }

  if (!values.value || values.value.trim().length === 0) {
    errors['value'] = 'Value cannot be empty';
  }

  if (!values.type || values.type.trim().length === 0) {
    errors['type'] = 'Type cannot be empty';
  }

  if (!values.description || values.description.trim().length === 0) {
    errors['description'] = 'Description cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // If no errors, execute API request
  return fetch(`${API_CONFIG.BASE_URL}/configurations/${params.id}`, {
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
