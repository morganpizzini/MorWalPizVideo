import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AccountMailPage, AccountEmailNotice } from "@morwalpiz/layout";
import { I18nProvider } from "../i18n";
import AccountMail from "./AccountMail";

const requests = vi.hoisted(() => ({
  reset: vi.fn().mockResolvedValue(undefined),
  confirmation: vi.fn().mockResolvedValue(undefined),
}));
vi.mock("@morwalpizvideo/services", () => ({
  requestPublicPasswordReset: requests.reset,
  requestPublicEmailConfirmation: requests.confirmation,
  resetPublicPassword: vi.fn(),
  confirmPublicEmail: vi.fn(),
  getPublicAccountMailErrorMessage: () => "Mail unavailable",
}));

afterEach(() => { cleanup(); vi.useRealTimers(); vi.clearAllMocks(); });
const t = (message: string) => message;
const errorMessage = () => "Mail unavailable";

describe("shared account email UI", () => {
  it("enables resend at exactly 60 seconds and recomputes from elapsed time", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-11-26T22:00:00Z"));
    const onRequest = vi.fn().mockResolvedValue(undefined);
    render(<MemoryRouter><AccountMailPage mode="request-reset" t={t} onRequest={onRequest}
      onConsume={vi.fn()} errorMessage={errorMessage} /></MemoryRouter>);
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "mail@example.test" } });
    await act(async () => { fireEvent.submit(screen.getByRole("button", { name: "Send email" }).closest("form")!); });
    expect(onRequest).toHaveBeenCalledWith("mail@example.test");
    expect((screen.getByRole("button", { name: "Request again in 60s" }) as HTMLButtonElement).disabled).toBe(true);
    act(() => { vi.advanceTimersByTime(59_000); });
    expect((screen.getByRole("button", { name: "Request again in 1s" }) as HTMLButtonElement).disabled).toBe(true);
    act(() => { vi.advanceTimersByTime(1_000); });
    expect((screen.getByRole("button", { name: "Request a new email" }) as HTMLButtonElement).disabled).toBe(false);
    await act(async () => { fireEvent.submit(screen.getByRole("button", { name: "Request a new email" }).closest("form")!); });
    vi.setSystemTime(new Date("2026-11-26T22:05:00Z"));
    act(() => { window.dispatchEvent(new Event("focus")); });
    expect((screen.getByRole("button", { name: "Request a new email" }) as HTMLButtonElement).disabled).toBe(false);
  });

  it("does not start a cooldown after a failure or dispatch confirmation on render", async () => {
    const onRequest = vi.fn().mockRejectedValue(new Error("offline"));
    render(<AccountEmailNotice t={t} onRequest={onRequest} onRefresh={vi.fn()} errorMessage={errorMessage} />);
    expect(onRequest).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Request a confirmation email" }));
    await waitFor(() => expect(screen.getByRole("alert").textContent).toBe("Mail unavailable"));
    expect((screen.getByRole("button", { name: "Request a confirmation email" }) as HTMLButtonElement).disabled).toBe(false);
  });

  it("does not consume a link on render or submit mismatched passwords", async () => {
    const onConsume = vi.fn().mockResolvedValue(undefined);
    const link = { accountId: "b".repeat(24), token: "A".repeat(64) };
    render(<MemoryRouter><AccountMailPage mode="reset" t={t} onRequest={vi.fn()} onConsume={onConsume}
      link={link} errorMessage={errorMessage} /></MemoryRouter>);
    expect(onConsume).not.toHaveBeenCalled();
    fireEvent.change(screen.getByLabelText("New password"), { target: { value: "new-password-123" } });
    fireEvent.change(screen.getByLabelText("Confirm password"), { target: { value: "different-password" } });
    fireEvent.submit(screen.getByRole("button", { name: "Save password" }).closest("form")!);
    expect(screen.getByRole("alert").textContent).toContain("Passwords do not match.");
    expect(onConsume).not.toHaveBeenCalled();
    fireEvent.change(screen.getByLabelText("Confirm password"), { target: { value: "new-password-123" } });
    fireEvent.submit(screen.getByRole("button", { name: "Save password" }).closest("form")!);
    await waitFor(() => expect(screen.getByRole("status").textContent).toContain("Password updated."));
    expect(onConsume).toHaveBeenCalledWith(link, "new-password-123");
    expect(screen.queryByLabelText("New password")).toBeNull();
  });

  it("Workshop requests use the Workshop site identifier", async () => {
    render(<MemoryRouter initialEntries={["/account/forgot-password"]}><I18nProvider><AccountMail /></I18nProvider></MemoryRouter>);
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "mail@example.test" } });
    fireEvent.submit(screen.getByRole("button", { name: "Send email" }).closest("form")!);
    await waitFor(() => expect(requests.reset).toHaveBeenCalledWith({ email: "mail@example.test", site: "workshop" }));
  });
});
