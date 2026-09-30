import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { BayForm, ConfigForm } from "./Admin";
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
  it.each([0, 1, 2] as const)(
    "renders only relevant windows for numeric mode %s",
    (mode) => {
      const html = renderToStaticMarkup(
        <ConfigForm
          initial={{ ...config, sessionMode: mode }}
          busy={false}
          onSave={async () => {}}
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
    },
  );

  it("blocks controls while saving and flags readable legacy duration", () => {
    const html = renderToStaticMarkup(
      <ConfigForm
        initial={{ ...config, hourlyMinutes: 45 }}
        busy
        onSave={async () => {}}
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
          mechanisms: [],
          partitions: 0,
          plates: 0,
          pepper: true,
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
        busy={false}
        onSave={async () => {}}
      />,
    );
    expect(html).toContain("Manutenzione");
    expect(html).toContain('type="number"');
    expect(html).toContain('type="checkbox"');
    expect(html).toContain("approved-user");
    expect(html).not.toContain("passwordHash");
  });

  it.each(["/", "/admin", "/messages"])(
    "supports direct SSR navigation to %s without exposing content before restoration",
    async (path) => {
      const result = await render(new Request(`https://localhost${path}`));
      expect(result.html).toContain("Ripristino sessione");
      expect(result.html).not.toContain("Le mie prenotazioni");
      expect(result.html).not.toContain("passwordHash");
    },
  );
});
