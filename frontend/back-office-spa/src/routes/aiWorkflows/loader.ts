import { getAiPromptDefinitions } from '@morwalpizvideo/services';
import type { AiPromptDefinition } from '@morwalpizvideo/models';

export default async function loader(): Promise<AiPromptDefinition[]> {
  const definitions = await getAiPromptDefinitions();
  if (!Array.isArray(definitions))
    throw new Response('Unable to load AI workflow definitions', { status: 502 });
  return definitions;
}
