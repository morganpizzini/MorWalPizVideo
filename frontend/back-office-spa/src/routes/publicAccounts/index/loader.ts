import { getPublicAccountDirectory } from '@morwalpizvideo/services';
import type { PublicAccountDirectoryPage } from '@morwalpizvideo/models';

export interface PublicAccountsIndexData {
  page: PublicAccountDirectoryPage | null;
  error: string | null;
}

export default async function loader({
  request,
}: {
  request: Request;
}): Promise<PublicAccountsIndexData> {
  const url = new URL(request.url);
  const search = url.searchParams.get('search') ?? '';
  const pageNumber = Number(url.searchParams.get('page') ?? '1');

  try {
    return {
      page: await getPublicAccountDirectory(search, Number.isInteger(pageNumber) ? pageNumber : 1),
      error: null,
    };
  } catch (error) {
    return {
      page: null,
      error: error instanceof Error ? error.message : 'Unable to load public accounts.',
    };
  }
}
