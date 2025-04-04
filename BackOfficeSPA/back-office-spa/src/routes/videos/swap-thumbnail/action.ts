import { ActionFunctionArgs, data } from 'react-router';
import { API_CONFIG } from '@config/api';

export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const values = {
    currentVideoId: formData.get('currentVideoId') as string,
    newVideoId: formData.get('newVideoId') as string,
  };

  const errors: Record<string, string | string[]> = {};

  // Field validation
  if (!values.currentVideoId || values.currentVideoId.trim().length === 0) {
    errors['currentVideoId'] = 'ID Video Corrente è obbligatorio';
  }

  if (!values.newVideoId || values.newVideoId.trim().length === 0) {
    errors['newVideoId'] = 'ID Nuovo Video è obbligatorio';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // API request
  return fetch(`${API_CONFIG.BASE_URL}/videos/swap-thumbnail`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(async response => {
      if (!response.ok) {
        const errorData = await response.json();
        errors['generics'] = [
          errorData.message || 'Si è verificato un errore durante il cambio della thumbnail',
        ];
        return data({ success: false, errors }, { status: response.status });
      }
      return data({ success: true }, { status: 200 });
    })
    .catch(error => {
      errors['generics'] = [
        error.message || 'Si è verificato un errore durante il cambio della thumbnail',
      ];
      return data({ success: false, errors }, { status: 500 });
    });
}
