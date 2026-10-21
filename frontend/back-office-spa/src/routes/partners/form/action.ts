import { data, type ActionFunctionArgs } from 'react-router';
import { createPartner, updatePartner } from '@morwalpizvideo/services';
import { PartnerStatus, type CreatePartnerDTO } from '@/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const payload: CreatePartnerDTO = {
    name: String(values.name ?? '').trim(),
    contactName: String(values.contactName ?? '').trim(),
    contactEmail: String(values.contactEmail ?? '').trim(),
    contactPhone: String(values.contactPhone ?? '').trim(),
    websiteUrl: String(values.websiteUrl ?? '').trim(),
    notes: String(values.notes ?? '').trim(),
    status: Number(values.status ?? PartnerStatus.Prospect) as PartnerStatus,
  };
  if (!payload.name)
    return data({ success: false, errors: { name: 'Name is required.' } }, { status: 400 });
  try {
    if (params.id) await updatePartner(params.id, payload);
    else await createPartner(payload);
    return data({ success: true }, { status: params.id ? 200 : 201 });
  } catch {
    return data(
      { success: false, errors: { generics: ['Unable to save the partner.'] } },
      { status: 500 }
    );
  }
}
