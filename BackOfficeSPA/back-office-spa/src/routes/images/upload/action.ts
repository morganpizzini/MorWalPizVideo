import { ActionFunctionArgs, data } from 'react-router';


export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const image = formData.get('image') as File;
  const folderName = formData.get('folderName') as string;
  const loadInMatchFolder = formData.get('loadInMatchFolder') === 'true';

  const errors: Record<string, string | string[]> = {};

  // Field validation
  if (!image || image.size === 0) {
    errors['image'] = 'Please select a valid image file';
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
  apiFormData.append('image', image);
  apiFormData.append('folderName', folderName);
  apiFormData.append('loadInMatchFolder', loadInMatchFolder.toString());

  // API request
  return fetch(`/api/ImageUpload/upload`, {
    method: 'POST',
    body: apiFormData,
  })
    .then(async response => {
      if (!response.ok) {
        try {
          const errorData = await response.json();
          errors['generics'] = [errorData.message || 'Failed to upload image'];
        } catch (e) {
          errors['generics'] = ['Failed to upload image'];
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