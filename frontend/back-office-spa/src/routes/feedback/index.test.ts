import { beforeEach, describe, expect, it, vi } from 'vitest';

const { getFeedbackPage } = vi.hoisted(() => ({ getFeedbackPage: vi.fn() }));
vi.mock('@morwalpizvideo/services', () => ({ getFeedbackPage }));

import { loader } from './index';

describe('feedback management loader', () => {
  beforeEach(() => getFeedbackPage.mockReset());

  it('passes filters and pagination to the admin transport', async () => {
    getFeedbackPage.mockResolvedValue({ items: [], page: 2, pageSize: 10, total: 0 });
    await loader({ request: new Request('https://admin.test/feedback?page=2&pageSize=10&status=Reviewing&application=Workshop') });
    expect(getFeedbackPage).toHaveBeenCalledWith('Reviewing', 'Workshop', 2, 10);
  });

  it('rejects invalid pagination instead of issuing an unbounded request', async () => {
    await expect(loader({ request: new Request('https://admin.test/feedback?page=abc') })).rejects.toMatchObject({ status: 400 });
    expect(getFeedbackPage).not.toHaveBeenCalled();
  });
});
