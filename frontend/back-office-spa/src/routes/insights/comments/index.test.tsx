import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router';
import { InsightCommentSourceType, InsightSourceKind } from '@morwalpizvideo/models';

vi.mock('@morwalpizvideo/services', () => ({
  get: vi.fn().mockResolvedValue([]),
  endpoints: { CHANNELS_ACCESSIBLE: '/api/channels/accessible' },
  insightsTopicsApi: { getAll: vi.fn().mockResolvedValue([{ id: 'topic-1', title: 'Topic', description: '', seedArguments: [], preferredSources: [], creationDateTime: '' }]), analyzeComments: vi.fn() },
}));

import InsightCommentsPage from './index';

describe('insight comments source controls', () => {
  it('renders only the direct ID input for DirectVideoId', async () => {
    render(<MemoryRouter><InsightCommentsPage /></MemoryRouter>);
    await userEvent.selectOptions(await screen.findByLabelText('Source'), String(InsightCommentSourceType.DirectVideoId));
    expect(await screen.findByLabelText('Direct YouTube video ID')).toBeInTheDocument();
    expect(screen.queryByText('Select a stored video')).not.toBeInTheDocument();
    expect(screen.getByText('Create a topic')).toBeInTheDocument();
    expect(InsightCommentSourceType.DirectVideoId).toBe(2);
    expect(InsightSourceKind.ShortContent).toBe(1);
  });
});