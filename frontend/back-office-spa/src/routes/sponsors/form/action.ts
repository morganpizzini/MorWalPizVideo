import { createSponsorWithImage, updateSponsorWithImage } from '@morwalpizvideo/services';
import type { ActionFunctionArgs } from 'react-router';

export interface SponsorActionResult {
  success: boolean;
  errors?: {
    generics?: string[];
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function getGenericErrors(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.map(error => (typeof error === 'string' ? error : JSON.stringify(error)));
  }

  if (isRecord(value) && Array.isArray(value.generics)) {
    return value.generics.map(error => String(error));
  }

  return value == null ? [] : [String(value)];
}

function normalizeServiceResult(result: unknown): SponsorActionResult {
  if (!isRecord(result)) {
    return { success: true };
  }

  const genericErrors = getGenericErrors(result.errors);
  const status = typeof result.status === 'number' ? result.status : undefined;

  if (genericErrors.length > 0 || (status !== undefined && status >= 400)) {
    return {
      success: false,
      errors: { generics: genericErrors.length > 0 ? genericErrors : ['Unable to save sponsor'] },
    };
  }

  return { success: true };
}

export default async function action({
  request,
  params,
}: ActionFunctionArgs): Promise<SponsorActionResult> {
  const formData = await request.formData();

  try {
    const result = params.sponsorId
      ? await updateSponsorWithImage(params.sponsorId, formData)
      : await createSponsorWithImage(formData);

    return normalizeServiceResult(result);
  } catch (error) {
    console.error('Error saving sponsor:', error);
    return {
      success: false,
      errors: {
        generics: [error instanceof Error ? error.message : 'Failed to save sponsor'],
      },
    };
  }
}
