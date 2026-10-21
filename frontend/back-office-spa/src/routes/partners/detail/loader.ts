import type { LoaderFunctionArgs } from 'react-router';
import { getPartner } from '@morwalpizvideo/services';
import type { Partner } from '@/models';

export default ({ params }: LoaderFunctionArgs): Promise<Partner> => getPartner(params.id!);
