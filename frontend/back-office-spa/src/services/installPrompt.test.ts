import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import {
  getInstallPromptSnapshot,
  resetInstallPromptForTests,
  subscribeToInstallPrompt,
  useInstallPrompt,
} from './installPrompt';

function dispatchInstallEvent(outcome: 'accepted' | 'dismissed' = 'accepted') {
  const prompt = vi.fn().mockResolvedValue(undefined);
  const event = Object.assign(new Event('beforeinstallprompt'), {
    prompt,
    userChoice: Promise.resolve({ outcome }),
  });
  window.dispatchEvent(event);
  return prompt;
}

describe('installPrompt', () => {
  beforeEach(() => {
    localStorage.clear();
    resetInstallPromptForTests();
  });

  afterEach(() => {
    window.dispatchEvent(new Event('appinstalled'));
  });

  it('captures the browser event and notifies subscribers', () => {
    const listener = vi.fn();
    const unsubscribe = subscribeToInstallPrompt(listener);

    dispatchInstallEvent();

    expect(getInstallPromptSnapshot()).toMatchObject({ canInstall: true, dismissed: false });
    expect(listener).toHaveBeenCalled();
    unsubscribe();
  });

  it('prompts only through an explicit install action and records acceptance', async () => {
    const prompt = dispatchInstallEvent();
    const { result } = renderHook(() => useInstallPrompt());

    await act(async () => {
      await expect(result.current.install()).resolves.toBe('accepted');
    });

    expect(prompt).toHaveBeenCalledOnce();
    expect(getInstallPromptSnapshot().canInstall).toBe(false);
  });

  it('persists dismissal and allows a deliberate reopen', () => {
    dispatchInstallEvent();
    const { result } = renderHook(() => useInstallPrompt());

    act(() => result.current.dismiss());
    expect(getInstallPromptSnapshot().dismissed).toBe(true);
    expect(localStorage.getItem('backoffice.install-prompt-dismissed.v1')).toBe('true');

    act(() => result.current.reopen());
    expect(getInstallPromptSnapshot().dismissed).toBe(false);
  });

  it('hides the invitation after the app is installed', () => {
    dispatchInstallEvent();
    window.dispatchEvent(new Event('appinstalled'));

    expect(getInstallPromptSnapshot()).toMatchObject({ canInstall: false, installed: true });
  });
});
