import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Dialog } from "@morwalpiz/layout";

function ShootingDialogHarness() {
  const [notes, setNotes] = useState("");

  return (
    <Dialog open title="Record shooting" closeLabel="Close" onClose={() => undefined}>
      <label>
        Notes
        <input value={notes} onChange={(event) => setNotes(event.target.value)} />
      </label>
    </Dialog>
  );
}

describe("Shooting sessions dialog", () => {
  it("keeps the active input focused while controlled fields update", async () => {
    render(<ShootingDialogHarness />);
    await screen.findByRole("dialog", { name: "Record shooting" });

    const notes = screen.getByRole("textbox", { name: "Notes" });
    notes.focus();
    fireEvent.change(notes, { target: { value: "Range notes" } });

    await waitFor(() => expect(document.activeElement).toBe(notes));
    expect(notes).toHaveProperty("value", "Range notes");
  });
});
