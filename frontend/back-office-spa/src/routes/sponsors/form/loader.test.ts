import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getSponsor } from '@morwalpizvideo/services';
import loader from './loader';

vi.mock('@morwalpizvideo/services', () => ({ getSponsor: vi.fn() }));

describe('sponsor form loader', () => {
  beforeEach(() => vi.clearAllMocks());

  it('loads the sponsor using sponsorId', async () => {
    const sponsor = { id: 'sponsor-1', title: 'Sponsor' };
    vi.mocked(getSponsor).mockResolvedValue(sponsor as never);

    await expect(loader({ params: { sponsorId: 'sponsor-1' } } as never)).resolves.toBe(sponsor);
    expect(getSponsor).toHaveBeenCalledWith('sponsor-1');
  });

  it('returns null for the create route', async () => {
    await expect(loader({ params: {} } as never)).resolves.toBeNull();
    expect(getSponsor).not.toHaveBeenCalled();
  });
});
