// @vitest-environment jsdom
import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OpeningDayDatePicker } from "./OpeningDayDatePicker";

let root: Root;
let container: HTMLDivElement;

beforeEach(() => {
  Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
  container = document.createElement("div");
  document.body.append(container);
  root = createRoot(container);
});

afterEach(async () => {
  await act(async () => root.unmount());
  container.remove();
});

describe("OpeningDayDatePicker", () => {
  it("disables weekdays the field is configured to close and selects an open day", async () => {
    const onChange = vi.fn();
    await act(async () =>
      root.render(
        <OpeningDayDatePicker
          value="2030-01-15"
          openingDays={[0, 6]}
          onChange={onChange}
        />,
      ),
    );

    await act(async () =>
      container.querySelector("summary")?.dispatchEvent(
        new MouseEvent("click", { bubbles: true, cancelable: true }),
      ),
    );

    const closedMonday = container.querySelector<HTMLButtonElement>(
      '[data-date="2030-01-14"]',
    );
    const openSaturday = container.querySelector<HTMLButtonElement>(
      '[data-date="2030-01-12"]',
    );
    expect(closedMonday?.disabled).toBe(true);
    expect(openSaturday?.disabled).toBe(false);

    await act(async () => openSaturday?.click());
    expect(onChange).toHaveBeenCalledWith("2030-01-12");
    expect(container.querySelector("details")?.open).toBe(false);
  });
});
