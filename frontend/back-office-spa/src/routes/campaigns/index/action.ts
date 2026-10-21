import { data } from 'react-router';
import { deleteCampaign } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  try {
    await deleteCampaign(String(values.id));
    return data({ success: true }, { status: 200 });
  } catch {
    return data(
      { success: false, errors: { generics: ['Unable to delete the campaign.'] } },
      { status: 409 }
    );
  }
}
