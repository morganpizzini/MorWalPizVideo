import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { BayForm, ConfigForm } from "./Admin";
import { groupBookings } from "./AdminBookings";
import { render } from "./entry-server";
import type { RangeConfig } from "./api";

const config: RangeConfig = {
  id: "default",
  creationDateTime: "2030-01-01T00:00:00Z",
  name: "Pepperbox",
  timeZone: "Europe/Rome",
  sessionMode: 0,
  openingDays: [6, 0],
  morningStart: "09:00:00",
  morningEnd: "13:00:00",
  afternoonStart: "14:00:00",
  afternoonEnd: "18:00:00",
  hourlyMinutes: 60,
  continuousStart: null,
  continuousEnd: null,
  reservedReleaseDaysBefore: 2,
};

describe("admin form structure and SSR", () => {
  it("groups shared interval participants under local date, bay and stored time", () => {
    const makeBooking = (values: Partial<import("./api").Booking>): import("./api").Booking => ({
      id: "booking",
      creationDateTime: config.creationDateTime,
      userId: "user-1",
      bayId: "bay-1",
      localDate: "2030-01-13",
      periodKey: "09:00-11:00",
      startUtc: "2030-01-13T08:00:00Z",
      endUtc: "2030-01-13T10:00:00Z",
      request: "",
      status: 0,
      ...values,
    });
    const groups = groupBookings([
      makeBooking({ id: "later", localDate: "2030-01-14", startUtc: "2030-01-14T08:00:00Z" }),
      makeBooking({ id: "friend", status: 1 }),
      makeBooking({ id: "separate", periodKey: "11:00-13:00", startUtc: "2030-01-13T10:00:00Z", endUtc: "2030-01-13T12:00:00Z" }),
      makeBooking({ id: "owner", request: "Keep target setup ready" }),
    ]);

    expect(groups.map((group) => group.localDate)).toEqual(["2030-01-13", "2030-01-14"]);
    expect(groups[0].intervals).toHaveLength(2);
    expect(groups[0].intervals.find((interval) => interval.bookings.length === 2)?.bookings.map((item) => item.status)).toEqual([1, 0]);
    expect(groups[0].intervals.flatMap((interval) => interval.bookings).find((item) => item.id === "owner")?.request).toBe("Keep target setup ready");
  });

  it.each([0, 1, 2, 3] as const)(
    "renders only relevant windows for numeric mode %s",
    (mode) => {
      const html = renderToStaticMarkup(
        <ConfigForm
          initial={{ ...config, sessionMode: mode }}
          busy={false}
          onSave={async () => {}}
          onDraftChange={() => {}}
        />,
      );
      expect(html).toContain("Mattina o pomeriggio intero");
      expect(html).toContain("60 minuti, mattina e pomeriggio con pausa");
      expect(html).toContain("60 minuti, apertura continua");
      expect(html).toContain("Giorni di apertura");
      if (mode === 2) {
        expect(html).toContain("Apertura continua");
        expect(html).not.toContain("Apertura mattina");
        expect(html).toContain('value=""');
      } else {
        expect(html).toContain("Apertura mattina");
        expect(html).toContain("Chiusura pomeriggio");
        expect(html).not.toContain("Apertura continua");
      }
      if (mode === 3) {
        expect(html).toContain("Sessioni intere o intervalli consecutivi");
        expect(html).toContain("Durata intervallo");
        expect(html).toContain("120 minuti");
      }
    },
  );

  it("blocks controls while saving and flags readable legacy duration", () => {
    const html = renderToStaticMarkup(
      <ConfigForm
        initial={{ ...config, hourlyMinutes: 45 }}
        busy
        onSave={async () => {}}
        onDraftChange={() => {}}
      />,
    );
    expect(html).toContain('disabled=""');
    expect(html).toContain("Durata precedente non valida");
    expect(html).toContain("60 minuti");
    expect(html).toContain('role="alert"');
  });

  it("renders native bay state, quantity and whitelist controls without user hashes", () => {
    const html = renderToStaticMarkup(
      <BayForm
        initial={{
          id: "bay",
          creationDateTime: config.creationDateTime,
          code: "01",
          description: "",
          mechanisms: [{ type: "Pepper popper", quantity: 2 }],
          targets: 3,
          partitions: 0,
          plates: 0,
          pepper: 1,
          height: 1.5,
          width: 2.25,
          status: 1,
          reservedUntilUtc: null,
          whitelistUserIds: ["user"],
        }}
        users={[
          {
            id: "user",
            creationDateTime: config.creationDateTime,
            username: "approved-user",
            firstName: "Test",
            lastName: "User",
            status: 1,
            isAdmin: false,
            forcePasswordChange: false,
          },
        ]}
        mechanismTypes={[{ id: "type-1", name: "Pepper popper" }]}
        busy={false}
        onSave={async () => {}}
        onDraftChange={() => {}}
      />,
    );
    expect(html).toContain("Manutenzione");
    expect(html).toContain('type="number"');
    expect(html).toContain('type="checkbox"');
    expect(html).toContain("Pepper popper");
    expect(html).toContain("Bersagli");
    expect(html).toContain("Popper");
    expect(html).toContain("Altezza");
    expect(html).toContain("Larghezza");
    expect(html).toContain('class="bay-dimension-fields"');
    expect(html).toContain("<select");
    expect(html).toContain("approved-user");
    expect(html).not.toContain("passwordHash");
  });

  it.each(["/", "/admin", "/admin/bookings", "/messages"])(
    "supports direct SSR navigation to %s without exposing content before restoration",
    async (path) => {
      const result = await render(new Request(`https://localhost${path}`));
      expect(result.html).toContain("Ripristino sessione");
      expect(result.html).not.toContain("Le mie prenotazioni");
      expect(result.html).not.toContain("passwordHash");
    },
  );
});
