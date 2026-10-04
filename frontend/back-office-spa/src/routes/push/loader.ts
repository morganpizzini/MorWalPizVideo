import { getPushTargets } from '@morwalpizvideo/services';
import type { PushTargets } from '@morwalpizvideo/models';

export default async function loader(): Promise<PushTargets> {
  const targets = await getPushTargets();
  if (!targets || !Array.isArray(targets.channels))
    throw new Response('Unable to load push targets', { status: 502 });
  return targets;
}
