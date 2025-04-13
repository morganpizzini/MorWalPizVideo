import { ActionFunctionArgs } from 'react-router';
import { CreateCalendarEventRequest } from '@models/CalendarEvent';

export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const title = formData.get('title')?.toString() || '';
  const description = formData.get('description')?.toString() || '';
  const date = formData.get('date')?.toString() || '';
  const category = formData.get('category')?.toString() || '';
  const matchId = formData.get('matchId')?.toString() || '';

  // Validate required fields
  const errors: Record<string, string> = {};

  if (!title) {
    errors.title = 'Title is required';
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
  const calendarEvent: CreateCalendarEventRequest = {
    title,
    description,
    date,
    category,
    matchId
  };

  try {
    const response = await fetch('/api/calendarEvents', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(calendarEvent)
    });

    if (!response.ok) {
      const errorText = await response.text();
      return {
        success: false,
        errors: {
          generics: [errorText || `Failed to create calendar event. Status: ${response.status}`]
        }
      };
    }

    return { success: true };
  } catch (error) {
    return {
      success: false,
      errors: {
        generics: [(error as Error).message || 'An unexpected error occurred']
      }
    };
  }
}
