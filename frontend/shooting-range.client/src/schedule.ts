import type { Availability, Session } from "./api";

export function sessionLabel(session: Session, timeZone: string): string {
  const format = new Intl.DateTimeFormat("it-IT", {
    timeZone,
    hour: "2-digit",
    minute: "2-digit",
  });
  const name =
    session.periodKey.toLowerCase() === "morning"
      ? "Mattina "
      : session.periodKey.toLowerCase() === "afternoon"
        ? "Pomeriggio "
        : "";
  return `${name}${format.format(new Date(session.startUtc))} - ${format.format(new Date(session.endUtc))}`;
}

export function availabilityMatches(
  data: Availability,
  offered: Session[],
  date: string,
  period: string,
): boolean {
  return (
    data.localDate === date &&
    data.periodKey === period &&
    offered.some(
      (session) =>
        session.periodKey === period &&
        session.startUtc === data.startUtc &&
        session.endUtc === data.endUtc,
    )
  );
}

export const accountStatus = (status: number): string =>
  ["In attesa", "Approvato", "Disabilitato"][status] ?? "Stato sconosciuto";
export const bookingStatus = (status: number): string =>
  ["In attesa", "Approvata", "Rifiutata", "Annullata"][status] ??
  "Stato sconosciuto";
