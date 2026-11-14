import { describe, expect, it } from 'vitest';

describe('Request test harness', () => {
  it('preserves abort propagation for router requests', () => {
    const controller = new AbortController();
    const request = new Request('https://admin.test/feedback', { signal: controller.signal });

    expect(request.signal.aborted).toBe(false);
    controller.abort();
    expect(request.signal.aborted).toBe(true);
  });
});
