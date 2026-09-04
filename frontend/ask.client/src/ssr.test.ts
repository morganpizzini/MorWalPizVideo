import { beforeEach, describe, expect, it, vi } from 'vitest';

const { getAskCampaign } = vi.hoisted(() => ({ getAskCampaign: vi.fn() }));

vi.mock('@morwalpizvideo/services', () => ({
  getAskCampaign,
  setRequestCredentialsMode: vi.fn(),
}));

import { render } from './entry-server';
import { createAskMetadata, loader } from './routes';

const campaign = {
  channelName: 'Scenario channel',
  title: 'Ask the team',
  description: 'Send a question to the team.',
  slug: 'spring-questions',
  maxSubmissionLength: 2000,
  allowNamedSubmissions: false,
  nameRequired: false,
  recaptchaRequired: false,
  questions: [],
};

describe('Ask SSR runtime', () => {
  beforeEach(() => getAskCampaign.mockReset());

  it('loads a campaign and renders metadata and content', async () => {
    getAskCampaign.mockResolvedValue(campaign);

    const result = await render(new Request('https://ask.example/Scenario%20channel/spring-questions'));

    expect(getAskCampaign).toHaveBeenCalledWith('Scenario channel', 'spring-questions');
    expect(createAskMetadata(campaign, 'https://ask.example')).toEqual({
      title: 'Ask the team | Ask',
      description: 'Send a question to the team.',
      canonical: 'https://ask.example/Scenario%20channel/spring-questions',
    });
    expect(result.html).toContain('Ask the team');
  });

  it('returns a router 404 when route parameters are missing', async () => {
    await expect(loader({ params: {} } as never)).rejects.toMatchObject({ status: 404 });
    expect(getAskCampaign).not.toHaveBeenCalled();
  });

  it('propagates API 404 for closed or missing campaigns', async () => {
    getAskCampaign.mockRejectedValue(new Response('Not found', { status: 404 }));

    const result = await render(new Request('https://ask.example/Scenario%20channel/closed'));

    expect(result.html).toContain('404');
  });

  it('propagates non-404 API failures to the SSR host', async () => {
    getAskCampaign.mockRejectedValue(new Error('API unavailable'));

    const result = await render(new Request('https://ask.example/Scenario%20channel/failure'));

    expect(result.html).toContain('API unavailable');
  });
});