import { CalendarEvent } from '@morwalpizvideo/models';
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  try {
    const events: CalendarEvent[] = await get(endpoints.CALENDAREVENTS);
    return events || [];
  } catch (error) {
    console.error('Failed to load calendar events:', error);
    return [];
  }
}
