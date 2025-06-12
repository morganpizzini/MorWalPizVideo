import { CalendarEvent } from '@models/CalendarEvent';
export default async function loader() {
  try {
    const response = await fetch(`/api/calendarEvents`);

    if (!response.ok) {
      throw new Error(`HTTP error! Status: ${response.status}`);
    }
    console.log(response)
    const events: CalendarEvent[] = await response.json();
    return events;
  } catch (error) {
    console.error('Failed to load calendar events:', error);
    return [];
  }
}
