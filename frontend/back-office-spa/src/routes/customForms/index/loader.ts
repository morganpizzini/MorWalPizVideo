import { CustomForm } from '@morwalpizvideo/models';

export default async function loader() {
  try {
    const response = await fetch(`/api/customforms`);

    if (!response.ok) {
      throw new Error(`HTTP error! Status: ${response.status}`);
    }

    const forms: CustomForm[] = await response.json();
    return forms;
  } catch (error) {
    console.error('Failed to load custom forms:', error);
    return [];
  }
}
