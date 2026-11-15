import type { SupportedCulture } from "./i18n";

export function formatNumber(value: number, culture: SupportedCulture) {
  return new Intl.NumberFormat(culture).format(value);
}

export function formatDate(value: string | Date, culture: SupportedCulture) {
  return new Intl.DateTimeFormat(culture, { dateStyle: "medium" }).format(new Date(value));
}

export function formatDateTime(value: string | Date, culture: SupportedCulture) {
  return new Intl.DateTimeFormat(culture, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function localDateInputValue(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function localDateTimeInputValue(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  const hours = String(date.getHours()).padStart(2, "0");
  const minutes = String(date.getMinutes()).padStart(2, "0");
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}
