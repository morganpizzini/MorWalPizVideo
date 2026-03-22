import { get, frontendEndpoints } from '@morwalpizvideo/services';

export function getCalendar() {
    return get(frontendEndpoints.CALENDAREVENTS);
}