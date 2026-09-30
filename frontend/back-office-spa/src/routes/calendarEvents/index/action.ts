import { data, type ActionFunctionArgs } from 'react-router';
import { deleteCalendarEvent, ApiResponseError } from '@morwalpizvideo/services';
import { calendarRevision, calendarActionError, type CalendarActionResult } from '../form';

export default async function action({ request }: ActionFunctionArgs) {
  try {
    const formData = await request.formData();
    const id = String(formData.get('id') ?? '');
    if (!id)
      throw new ApiResponseError({ status: 400, errors: ['Calendar event ID is required.'] });
    await deleteCalendarEvent(id, calendarRevision(formData));
    return data<CalendarActionResult>({ success: true }, { status: 200 });
  } catch (error) {
    return calendarActionError(error);
  }
}
