import type { BlogSnapshot, InsightArticleDraft } from '@morwalpizvideo/models';

const emptyDraft = (): BlogSnapshot => ({
  title: '',
  summary: '',
  author: '',
  tags: [],
  coverImageId: null,
  coverAlt: '',
  seoTitle: '',
  seoDescription: '',
  document: { version: 1, blocks: [] },
});

export function buildInsightBlogSnapshot(insightDraft: InsightArticleDraft): BlogSnapshot {
  return {
    ...emptyDraft(),
    title: insightDraft.title,
    summary: insightDraft.summary,
    seoTitle: insightDraft.title,
    seoDescription: insightDraft.summary,
    document: {
      version: 1,
      blocks: [
        {
          id: `insight-${insightDraft.topicId}`,
          type: 'richText',
          richText: {
            type: 'doc',
            content: [{ type: 'paragraph', content: [{ type: 'text', text: insightDraft.body }] }],
          },
        },
      ],
    },
  };
}
