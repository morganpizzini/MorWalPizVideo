import { data, type ActionFunctionArgs } from 'react-router';
import { createCampaign, updateCampaign } from '@morwalpizvideo/services';
import { CampaignStatus, type CreateCampaignDTO } from '@/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const payload: CreateCampaignDTO = {
    partnerId: String(values.partnerId ?? ''),
    title: String(values.title ?? '').trim(),
    description: String(values.description ?? '').trim(),
    status: Number(values.status ?? CampaignStatus.Draft) as CampaignStatus,
    startAt: String(values.startAt ?? '') || null,
    endAt: String(values.endAt ?? '') || null,
    contentReferences: String(values.contentReferences ?? '')
      .split(/\r?\n/)
      .map(value => value.trim())
      .filter(Boolean),
  };
  const errors: Record<string, string> = {};
  if (!payload.partnerId) errors.partnerId = 'Partner is required.';
  if (!payload.title) errors.title = 'Title is required.';
  if (Object.keys(errors).length) return data({ success: false, errors }, { status: 400 });
  try {
    if (params.id) await updateCampaign(params.id, payload);
    else await createCampaign(payload);
    return data({ success: true }, { status: params.id ? 200 : 201 });
  } catch {
    return data(
      { success: false, errors: { generics: ['Unable to save the campaign.'] } },
      { status: 500 }
    );
  }
}
