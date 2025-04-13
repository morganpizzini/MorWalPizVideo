import { ActionFunctionArgs } from 'react-router';
import { UpdateCalendarEventRequest } from '@models/CalendarEvent';

export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();

  // Original title is used to identify the event
  const title = formData.get('title')?.toString() || '';
  const newTitle = formData.get('newTitle')?.toString() || '';
  const description = formData.get('description')?.toString() || '';
  const date = formData.get('date')?.toString() || '';
  const category = formData.get('category')?.toString() || '';
  const matchId = formData.get('matchId')?.toString() || '';

  // Validate required fields
  const errors: Record<string, string> = {};

  if (!title) {
    errors.title = 'Original title is required';
  }

  if (!newTitle) {
    errors.newTitle = 'Title is required';
  }

  if (!date) {
    errors.date = 'Date is required';
  }

  if (Object.keys(errors).length > 0) {
    return {
      success: false,
      errors: {
        fields: errors,
        generics: ['Please correct the errors in the form']
      }
    };
  }

  // Create request payload
  const updateRequest: UpdateCalendarEventRequest = {
    title,
    newTitle,
    description,
    date,
    category,
    matchId
  };

  try {
    const response = await fetch('/api/calendarEvents', {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(updateRequest)
    });

    if (!response.ok) {
      const errorText = await response.text();
      return {
        success: false,
        errors: {
          generics: [errorText || `Failed to update calendar event. Status: ${response.status}`]
        }
      };
    }

    return {
      success: true,
      updatedTitle: newTitle // Return the new title so we can navigate to it
    };
  } catch (error) {
    return {
      success: false,
      errors: {
        generics: [(error as Error).message || 'An unexpected error occurred']
      }
    };
  }
}
