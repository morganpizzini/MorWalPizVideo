export interface CalendarEvent {
  id: string;
  title: string;
  description: string;
  date: string; // DateOnly from backend will be represented as string in ISO format
  category: string;
  matchId: string;
  calendarEventId: string; // This is the same as title
}

export interface CreateCalendarEventRequest {
  title: string;
  description: string;
  date: string; // DateOnly from backend will be represented as string in ISO format
  category: string;
  matchId: string;
}

export interface UpdateCalendarEventRequest {
  title: string;
  newTitle: string;
  description: string;
  date: string; // DateOnly from backend will be represented as string in ISO format
  category: string;
  matchId: string;
}
