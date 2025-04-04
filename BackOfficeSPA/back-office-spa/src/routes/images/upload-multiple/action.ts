import { ActionFunctionArgs, data } from 'react-router';
import { API_CONFIG } from '@config/api';

export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const images = formData.getAll('images') as File[];
  const folderName = formData.get('folderName') as string;
  const loadInMatchFolder = formData.get('loadInMatchFolder') === 'true';

  const errors: Record<string, string | string[]> = {};

  // Field validation
  if (!images || images.length === 0) {
    errors['images'] = 'Please select at least one image file';
  }

  if (!folderName || folderName.trim().length === 0) {
    errors['folderName'] = 'Please select a match folder';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // Create a new FormData for the API request
  const apiFormData = new FormData();

  // Add all images to the FormData
  for (const image of images) {
    apiFormData.append('images', image);
  }

  apiFormData.append('folderName', folderName);
  apiFormData.append('loadInMatchFolder', loadInMatchFolder.toString());

  // API request
  return fetch(`${API_CONFIG.BASE_URL}/ImageUpload/upload-multiple`, {
    method: 'POST',
    body: apiFormData,
  })
    .then(async response => {
      if (!response.ok) {
        try {
          const errorData = await response.json();
          errors['generics'] = [errorData.message || 'Failed to upload images'];
        } catch (e) {
          errors['generics'] = ['Failed to upload images'];
        }
        return data({ success: false, errors }, { status: response.status });
      }
      return data({ success: true }, { status: 201 });
    })
    .catch(error => {
      errors['generics'] = [error.message || 'API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}