import { CalendarEvent } from '@models/CalendarEvent';
import { API_CONFIG } from '@config/api';
export default async function loader() {
  try {
    const response = await fetch(`${API_CONFIG.BASE_URL}/calendarEvents`);

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
