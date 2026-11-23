// @vitest-environment jsdom
import { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, expect, it, vi } from "vitest";
import { BayForm } from "./Admin";
import type { AdminUser, Bay } from "./api";

afterEach(() => {
  vi.unstubAllGlobals();
  document.body.innerHTML = "";
});

it("filters users, adds a priority pill, and removes it from the bay draft", async () => {
  vi.stubGlobal("IS_REACT_ACT_ENVIRONMENT", true);
  const container = document.createElement("div");
  document.body.append(container);
  const root = createRoot(container);
  const bay: Bay = {
    id: "bay",
    creationDateTime: "2030-01-01T00:00:00Z",
    code: "01",
    description: "",
    mechanisms: [],
    targets: 0,
    partitions: 0,
    plates: 0,
    pepper: 0,
    height: null,
    width: null,
    status: 0,
    reservedUntilUtc: null,
    whitelistUserIds: ["user-1"],
  };
  const users: AdminUser[] = [
    {
      id: "user-1",
      creationDateTime: bay.creationDateTime,
      username: "ada",
      firstName: "Ada",
      lastName: "Lovelace",
      status: 1,
      isAdmin: false,
      forcePasswordChange: false,
    },
    {
      id: "user-2",
      creationDateTime: bay.creationDateTime,
      username: "grace",
      firstName: "Grace",
      lastName: "Hopper",
      status: 1,
      isAdmin: false,
      forcePasswordChange: false,
    },
  ];
  const onDraftChange = vi.fn();

  try {
    await act(async () => {
      root.render(
        <BayForm
          initial={bay}
          users={users}
          mechanismTypes={[]}
          busy={false}
          onSave={async () => {}}
          onDraftChange={onDraftChange}
        />,
      );
    });

    const search = container.querySelector<HTMLInputElement>('input[type="search"]');
    const select = container.querySelector<HTMLSelectElement>(
      'select[aria-label="Seleziona un utente prioritario"]',
    );
    expect(search).not.toBeNull();
    expect(select).not.toBeNull();

    await act(async () => {
      search!.value = "grace";
      search!.dispatchEvent(new Event("input", { bubbles: true }));
    });
    expect(select!.options).toHaveLength(2);
    expect(select!.options[1].textContent).toContain("Grace Hopper (@grace)");

    await act(async () => {
      select!.value = "user-2";
      select!.dispatchEvent(new Event("change", { bubbles: true }));
    });
    const addButton = Array.from(container.querySelectorAll("button")).find(
      (button) => button.textContent?.trim() === "Aggiungi",
    );
    expect(addButton).toBeDefined();
    await act(async () => addButton!.click());

    expect(onDraftChange).toHaveBeenLastCalledWith({
      ...bay,
      whitelistUserIds: ["user-1", "user-2"],
    });
    expect(container.textContent).toContain("Grace Hopper (@grace)");

    const removeButton = container.querySelector<HTMLButtonElement>(
      'button[aria-label="Rimuovi Ada Lovelace (@ada) dai prioritari"]',
    );
    expect(removeButton).not.toBeNull();
    await act(async () => removeButton!.click());
    expect(onDraftChange).toHaveBeenLastCalledWith({
      ...bay,
      whitelistUserIds: ["user-2"],
    });
  } finally {
    await act(async () => root.unmount());
  }
});
