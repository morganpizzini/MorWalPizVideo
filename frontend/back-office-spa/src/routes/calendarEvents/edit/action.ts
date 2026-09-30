import { data } from 'react-router';
import { updateCalendarEvent, ApiResponseError } from '@morwalpizvideo/services';
import { calendarPayload, calendarActionError, type CalendarActionResult } from '../form';

export default async function action({ request }: { request: Request }) {
  try {
    const payload = calendarPayload(await request.formData(), true);
    if (!payload.id)
      throw new ApiResponseError({ status: 400, errors: ['Calendar event ID is required.'] });
    const response = await updateCalendarEvent(payload.id, payload);
    return data<CalendarActionResult>(
      { success: true, updatedTitle: response.title },
      { status: 200 }
    );
  } catch (error) {
    return calendarActionError(error);
  }
}
