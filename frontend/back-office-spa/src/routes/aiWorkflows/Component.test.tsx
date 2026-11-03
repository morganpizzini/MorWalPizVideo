import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router';
import { ToastContext } from '@components/ToastNotification/ToastContext';
import type { AiPromptDefinition, AiPromptRevision } from '@morwalpizvideo/models';
import * as aiServices from '@morwalpizvideo/services';
import { useAppStore } from '../../state/appStore';

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: vi.fn(),
}));

vi.mock('@morwalpizvideo/services', () => ({
  bootstrapAiPrompts: vi.fn(),
  cancelAiPromptCanary: vi.fn(),
  createAiEvaluationCase: vi.fn(),
  createAiPromptCandidate: vi.fn(),
  createAiPromptRevision: vi.fn(),
  evaluateAiPrompt: vi.fn(),
  getAiExecutionRecords: vi.fn(),
  getAiPromptDefinitions: vi.fn(),
  getAiPromptExperiments: vi.fn(),
  getAiPromptRevisions: vi.fn(),
  promoteAiPromptCanary: vi.fn(),
  rollbackAiPrompt: vi.fn(),
  startAiPromptCanary: vi.fn(),
}));

const definition: AiPromptDefinition = {
  workflowKey: 'chat.video-review',
  stageKey: 'metadata',
  displayName: 'Video review metadata',
  responseSchema: 'review',
  activeRevisionId: 'revision-active',
  canaryPercentage: 0,
  allowedToolNames: [],
};

const revision: AiPromptRevision = {
  id: 'revision-active',
  workflowKey: definition.workflowKey,
  stageKey: definition.stageKey,
  messages: [
    { role: 'system', template: 'Review policy from approved prompt.' },
    { role: 'user', template: '{{payload}}' },
  ],
  variables: ['payload'],
  responseSchema: 'review',
  allowedToolNames: [],
  rationale: 'Initial baseline',
  authorId: 'operator',
  contentHash: 'abcdef0123456789',
  lifecycle: 'Active',
  createdAtUtc: '2026-11-02T00:00:00Z',
};

describe('AI workflow operations page', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    useAppStore.setState({ effectivePermissions: ['ai.prompts.view'] });
    const { useResolvedLoaderData } = await import('@/router/asyncData');
    vi.mocked(useResolvedLoaderData).mockReturnValue([definition]);
    vi.mocked(aiServices.getAiPromptRevisions).mockResolvedValue([revision]);
    vi.mocked(aiServices.getAiPromptExperiments).mockResolvedValue([]);
    vi.mocked(aiServices.getAiExecutionRecords).mockResolvedValue([]);
  });

  it('shows immutable baseline content and metadata-only health for view permission', async () => {
    const { default: Component } = await import('./Component');

    render(
      <MemoryRouter>
        <ToastContext.Provider value={{ show: vi.fn() }}>
          <Component />
        </ToastContext.Provider>
      </MemoryRouter>
    );

    fireEvent.click(screen.getByRole('button', { name: /Video review metadata/ }));

    expect(
      await screen.findByRole('heading', { name: 'Video review metadata' })
    ).toBeInTheDocument();
    expect(await screen.findByLabelText('system message')).toHaveValue(
      'Review policy from approved prompt.'
    );
    expect(
      screen.getByText(
        'Metadata only. Prompt, transcript, output, tool payloads, and credentials are not displayed.'
      )
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save immutable draft' })).toBeDisabled();
    await waitFor(() =>
      expect(aiServices.getAiPromptRevisions).toHaveBeenCalledWith(
        definition.workflowKey,
        definition.stageKey
      )
    );
  }, 30000);
});
