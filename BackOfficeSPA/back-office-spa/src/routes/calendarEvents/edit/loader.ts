import { LoaderFunctionArgs } from 'react-router';
import { CalendarEvent } from '@models/CalendarEvent';

export default async function loader({ params }: LoaderFunctionArgs) {
  const { title } = params;

  if (!title) {
    throw new Response('Title is required', { status: 400 });
  }

  try {
    const response = await fetch(`/api/calendarEvents/${encodeURIComponent(title)}`);

    if (response.status === 404) {
      throw new Response('Calendar event not found', { status: 404 });
    }

    if (!response.ok) {
      throw new Response(`Failed to load calendar event. Status: ${response.status}`, { status: response.status });
    }

    const calendarEvent: CalendarEvent = await response.json();
    return calendarEvent;
  } catch (error) {
    if (error instanceof Response) {
      throw error;
    }
    throw new Response((error as Error).message || 'An unexpected error occurred', { status: 500 });
  }
}
