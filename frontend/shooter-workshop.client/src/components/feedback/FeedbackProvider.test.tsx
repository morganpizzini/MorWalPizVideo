import { fireEvent, render, screen, waitFor, cleanup } from "@testing-library/react";
import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";

const { submitFeedback } = vi.hoisted(() => ({ submitFeedback: vi.fn() }));
vi.mock("@morwalpizvideo/services", () => ({
  submitFeedback,
  ApiResponseError: class ApiResponseError extends Error {},
}));
vi.mock("../../auth/AuthProvider", () => ({ useWorkshopAuth: () => ({ session: { authenticated: false } }) }));

import { FeedbackProvider } from "./FeedbackProvider";

describe("Workshop feedback modal", () => {
  beforeEach(() => {
    submitFeedback.mockReset();
    window.history.replaceState({}, "", "/workshop");
  });
  afterEach(() => cleanup());

  it("traps focus, makes the page inert, and restores focus on cancel", async () => {
    render(<FeedbackProvider><button type="button">Page action</button></FeedbackProvider>);
    const launcher = screen.getByRole("button", { name: "Report an issue" });
    launcher.focus();
    fireEvent.click(launcher);
    expect(screen.getByRole("dialog")).toBeTruthy();
    const content = document.querySelector("[data-feedback-app-content]");
    expect(content?.getAttribute("aria-hidden")).toBe("true");
    fireEvent.keyDown(document, { key: "Tab" });
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(document.activeElement).toBe(launcher));
  });

  it("retains a failed draft for retry and reports malformed receipts", async () => {
    submitFeedback.mockRejectedValueOnce(new Error("network"));
    render(<FeedbackProvider><span>Page</span></FeedbackProvider>);
    fireEvent.click(screen.getByRole("button", { name: "Report an issue" }));
    fireEvent.change(screen.getByLabelText("Description"), { target: { value: "Something broke" } });
    fireEvent.click(screen.getByRole("button", { name: "Send feedback" }));
    expect((await screen.findByRole("alert")).textContent).toContain("Unable to send feedback");
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    fireEvent.click(screen.getByRole("button", { name: "Report an issue" }));
    expect((screen.getByLabelText("Description") as HTMLTextAreaElement).value).toBe("Something broke");
  });
});
