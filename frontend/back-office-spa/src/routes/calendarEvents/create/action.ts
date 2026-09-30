import { data } from 'react-router';
import { createCalendarEvent } from '@morwalpizvideo/services';
import { calendarPayload, calendarActionError, type CalendarActionResult } from '../form';

export default async function action({ request }: { request: Request }) {
  try {
    await createCalendarEvent(calendarPayload(await request.formData(), false));
    return data<CalendarActionResult>({ success: true }, { status: 201 });
  } catch (error) {
    return calendarActionError(error);
  }
}
