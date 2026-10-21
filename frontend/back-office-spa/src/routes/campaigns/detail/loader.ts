import type { LoaderFunctionArgs } from 'react-router';
import { getCampaign } from '@morwalpizvideo/services';
import type { Campaign } from '@/models';

export default ({ params }: LoaderFunctionArgs): Promise<Campaign> => getCampaign(params.id!);
