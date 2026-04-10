import { data } from 'react-router';
import { post, endpoints } from '@morwalpizvideo/services';
import { CreateCategoryDTO } from '@/models';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData()) as CreateCategoryDTO;
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.description || values.description.trim().length === 0) {
    errors['description'] = 'Description cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // If no errors, execute API request
  try {
    await post(endpoints.CATEGORIES, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
