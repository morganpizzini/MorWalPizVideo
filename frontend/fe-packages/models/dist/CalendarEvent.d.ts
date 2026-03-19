import { CategoryRef } from './video/types';
export interface CalendarEvent {
    id: string;
    title: string;
    description: string;
    startDate: string;
    endDate: string;
    categories: CategoryRef[];
    matchId: string;
    calendarEventId: string;
}
export interface CreateCalendarEventRequest {
    title: string;
    description: string;
    startDate: string;
    endDate: string;
    categoryIds: string[];
    matchId: string;
}
export interface UpdateCalendarEventRequest {
    title: string;
    newTitle: string;
    description: string;
    startDate: string;
    endDate: string;
    categoryIds: string[];
    matchId: string;
}
//# sourceMappingURL=CalendarEvent.d.ts.map