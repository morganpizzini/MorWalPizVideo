import { CategoryRef } from './video/types';

export interface CalendarEvent {
  id: string;
  title: string;
  description: string;
  startDate: string; // DateOnly from backend will be represented as string in ISO format
  endDate: string; // DateOnly from backend will be represented as string in ISO format
  categories: CategoryRef[];
  matchId: string;
  calendarEventId: string; // This is the same as title
}

export interface CreateCalendarEventRequest {
  title: string;
  description: string;
  startDate: string; // DateOnly from backend will be represented as string in ISO format
  endDate: string; // DateOnly from backend will be represented as string in ISO format
  categoryIds: string[];
  matchId: string;
}

export interface UpdateCalendarEventRequest {
  title: string;
  newTitle: string;
  description: string;
  startDate: string; // DateOnly from backend will be represented as string in ISO format
  endDate: string; // DateOnly from backend will be represented as string in ISO format
  categoryIds: string[];
  matchId: string;
}
