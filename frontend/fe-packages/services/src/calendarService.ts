import type {
  AdminCalendarEvent,
  CategoryRef,
  SaveCalendarEventRequest,
} from "@morwalpizvideo/models";
import {
  get,
  post,
  put,
  Delete,
  requireSuccessfulResponse,
} from "./apiService";
import endpoints, { ComposeUrl } from "./endpoints";

const identityUrl = (id: string) =>
  ComposeUrl(endpoints.CALENDAREVENTS_ID, { id: encodeURIComponent(id) });

export const getCalendarEventByTitle = async (
  title: string,
): Promise<AdminCalendarEvent> =>
  requireSuccessfulResponse(
    await get(
      ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, {
        title: encodeURIComponent(title),
      }),
    ),
  );
export const getCalendarEventById = async (
  id: string,
): Promise<AdminCalendarEvent> =>
  requireSuccessfulResponse(await get(identityUrl(id)));
export const fetchCalendarCategories = async (): Promise<CategoryRef[]> =>
  requireSuccessfulResponse(await get(endpoints.CATEGORIES));
export const createCalendarEvent = async (
  payload: SaveCalendarEventRequest,
): Promise<AdminCalendarEvent> =>
  requireSuccessfulResponse(await post(endpoints.CALENDAREVENTS, payload));
export const updateCalendarEvent = async (
  id: string,
  payload: SaveCalendarEventRequest,
): Promise<AdminCalendarEvent> =>
  requireSuccessfulResponse(await put(identityUrl(id), payload));
export const deleteCalendarEvent = async (
  id: string,
  revision: number,
): Promise<void> =>
  requireSuccessfulResponse(
    await Delete(`${identityUrl(id)}?revision=${revision}`),
  );
